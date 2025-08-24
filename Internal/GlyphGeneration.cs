using TextMeshDOTS.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using TextMeshDOTS.HarfBuzz;
using Unity.Burst.CompilerServices;
using UnityEngine;
using Font = TextMeshDOTS.HarfBuzz.Font;
using UnityEngine;


namespace TextMeshDOTS
{

    internal static class GlyphGeneration
    {
        internal static unsafe void CreateRenderGlyphs(ref FontTable fontTable,
                                                       ref GlyphTable glyphTable,
                                                       int threadIndex,
                                                       ref FontAssetArray fontAssetArray,
                                                       ref ComponentLookup<DynamicFontAsset> dynamicFontAssetsLookup,
                                                       ref ComponentLookup<FontAssetRef> fontAssetRefLookup,
                                                       ref DynamicBuffer<RenderGlyph> renderGlyphs,
                                                       ref DynamicBuffer<RenderGlyphOld> renderGlyphsOld,
                                                       in DynamicBuffer<CalliByte> calliBytesBuffer,
                                                       in DynamicBuffer<GlyphOTF> glyphOTFBuffer,
                                                       in DynamicBuffer<XMLTag> xmlTagBuffer,
                                                       in TextBaseConfiguration textBaseConfiguration,
                                                       ref TextColorGradientArray textColorGradientArray)
        {
            //Debug.Log("CreateRenderGlyphs");
            renderGlyphsOld.Clear();
            renderGlyphs.Clear();
            if (glyphOTFBuffer.IsEmpty)
                return;

            renderGlyphsOld.Capacity = glyphOTFBuffer.Length;   //2x speedup compared to allocation of individual items
            renderGlyphs.Capacity = glyphOTFBuffer.Length;      //2x speedup compared to allocation of individual items

          var calliString = new CalliString(calliBytesBuffer);
            var characters = calliString.GetEnumerator();

            var fontAssetRefs = fontAssetArray.fontAssetRefs;
            var layoutConfig = new LayoutConfig(in textBaseConfiguration);

            XMLTag currentTag=default;
            int tagsCounter = 0;
            int nextSegmentEndID = xmlTagBuffer.Length > 0 ? xmlTagBuffer[tagsCounter].startID : calliString.Length;
            int cleanedSegmentLength = nextSegmentEndID - currentTag.endID;
            int richTextOffset = 0;
            int nextTagPositionInCleanedText = cleanedSegmentLength;
            //Debug.Log($"{currentTag.tagType} {cleanedSegmentLength} {nextTagPositionInCleanedText}");

            int lastWordStartCharacterGlyphIndex = 0;
            FixedList512Bytes<int> characterGlyphIndicesWithPreceedingSpacesInLine = default;
            int startOfLineGlyphIndex = 0;
            int lastCommittedStartOfLineGlyphIndex = -1;
            bool isFirstLine = true;
            bool isLineStart = true;
            float currentLineHeight = 0f;
            float ascentLineDelta = 0;
            float decentLineDelta = 0;
            float accumulatedVerticalOffset = 0f;
            float maxLineAscender = float.MinValue;
            float maxLineDescender = float.MaxValue;

            var currentFaceIndex = glyphOTFBuffer[0].glyphKey.faceIndex;
            Entity currentFontEntity = fontTable.faceIndexToFontEntityMap[currentFaceIndex];
            var currentFontAssetRef = fontAssetRefLookup[currentFontEntity];
            var currentFont = fontTable.GetOrCreateFont(currentFaceIndex, threadIndex);
            var currentFontSamplingPointSize = FontTextureSize.Normal.GetSamplingSize();
            currentFont.SetScale(currentFontSamplingPointSize, currentFontSamplingPointSize);
            currentFont.UpdateMetaData();

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.            
            float baseScale = textBaseConfiguration.fontSize / currentFontSamplingPointSize * (textBaseConfiguration.isOrthographic ? 1 : 0.1f);
            float currentElementScale = baseScale;
            float currentEmScale = textBaseConfiguration.fontSize * 0.01f * (textBaseConfiguration.isOrthographic ? 1 : 0.1f);

            float topAnchor = GetTopAnchorForConfig(ref currentFont, textBaseConfiguration.verticalAlignment, baseScale);
            float bottomAnchor = GetBottomAnchorForConfig(ref currentFont, textBaseConfiguration.verticalAlignment, baseScale);

            Unicode.Rune currentRune, previousRune = Unicode.BadRune;//input text unicode

            for (int k = 0, length = glyphOTFBuffer.Length; k < length; k++)
            {
                var glyphOTF = glyphOTFBuffer[k];
                var glyphOTFFontEntity = fontTable.faceIndexToFontEntityMap[glyphOTF.glyphKey.faceIndex];

                var cluster = (int)glyphOTF.cluster; //cluster is char index in cleaned text = aligned with glyphOTF buffer
                if (currentFontEntity != glyphOTFFontEntity)
                {
                    currentFontEntity = glyphOTFFontEntity;
                    currentFontAssetRef = fontAssetRefLookup[currentFontEntity];
                    currentFont = fontTable.GetOrCreateFont(currentFaceIndex, threadIndex);
                    currentFont.SetScale(currentFontSamplingPointSize, currentFontSamplingPointSize);
                    currentFont.UpdateMetaData();
                }
                while (cluster >= nextTagPositionInCleanedText)
                {
                    if(tagsCounter < xmlTagBuffer.Length)
                    {                        
                        currentTag = xmlTagBuffer[tagsCounter++];
                        richTextOffset += currentTag.Length;
                        layoutConfig.Update(ref currentTag, textBaseConfiguration, ref textColorGradientArray);                       
                        nextSegmentEndID = tagsCounter < xmlTagBuffer.Length ? xmlTagBuffer[tagsCounter].startID - 1 : calliString.Length;
                        cleanedSegmentLength = nextSegmentEndID - currentTag.endID;                        
                        nextTagPositionInCleanedText = cluster + cleanedSegmentLength;
                        
                        //Debug.Log($"{currentTag.tagType} {cleanedSegmentLength} {nextTagPositionInCleanedText}");
                    }
                }

                // need to add richTextOffset to fetch correct char from richtext buffer. 
                // note: upper/lowercase is not applied in richtextBuffer (is only applied to cleaned text just before shaping)...should not cause any issues here
                characters.GotoByteIndex(richTextOffset + cluster); 
                currentRune = characters.Current;

                if (isFirstLine)
                    topAnchor = GetTopAnchorForConfig(ref currentFont, textBaseConfiguration.verticalAlignment, baseScale, topAnchor);
                bottomAnchor = GetBottomAnchorForConfig(ref currentFont, textBaseConfiguration.verticalAlignment, baseScale, bottomAnchor);

                #region Look up Character Data
                var glyphID = glyphTable.glyphHashToIdMap[glyphOTF.glyphKey];
                var glyphEntry = glyphTable.GetEntry(glyphID);
                //Debug.Log($"Render Glyph {glyphEntry.key.glyphIndex} from face {currentFaceIndex} using rect {glyphEntry.x} {glyphEntry.y} {glyphEntry.width} {glyphEntry.height} ({glyphEntry.PaddedWidth} {glyphEntry.PaddedHeight})");
                // review how to handle glyphOTF.codepoint = 0 (not defined glyph) which is retured for example for tab stop (9)
                // see here why: https://github.com/harfbuzz/harfbuzz/commit/81ef4f407d9c7bd98cf62cef951dc538b13442eb#commitcomment-9469767
                // should not be rendered, but xAdvance should be processed

                // Cache glyph metrics
                int x_bearing = glyphEntry.xBearing;
                int y_bearing   = glyphEntry.yBearing;
                int glyphHeight = glyphEntry.height;
                int glyphWidth  = glyphEntry.width;
                int padding = glyphEntry.padding;

                float adjustedScale = layoutConfig.m_currentFontSize / currentFontSamplingPointSize * (textBaseConfiguration.isOrthographic ? 1 : 0.1f);
                float elementAscentLine = currentFont.fontExtents.ascender;
                float elementDescentLine = currentFont.fontExtents.descender;

                //synthesize superscript and subscript redundant to opentype feature set during shaping.
                //only purpose is to simulate missing subscript glyphs, but unclear how to determine this
                float fontScaleMultiplier = 1;
                float m_subAndSupscriptOffset = 0;
                //if ((layoutConfiguration.m_fontStyles & FontStyles.Subscript) == FontStyles.Subscript && !currentRune.IsDigit())
                //{
                //    //Debug.Log($"{currentFont.subScriptEmXSize} {currentFont.subScriptEmYOffset} {adjustedScale}");
                //    fontScaleMultiplier = currentFont.subScriptEmXSize * adjustedScale;
                //    m_SubAndSupscriptOffset = -currentFont.subScriptEmYOffset * adjustedScale;
                //}
                //else if ((layoutConfiguration.m_fontStyles & FontStyles.Superscript) == FontStyles.Superscript && !currentRune.IsDigit())
                //{
                //    fontScaleMultiplier = currentFont.superScriptEmXSize * adjustedScale;
                //    m_SubAndSupscriptOffset = currentFont.superScriptEmYOffset * adjustedScale;
                //}

                currentElementScale = adjustedScale * fontScaleMultiplier;
                float baselineOffset = currentFont.baseLine * adjustedScale * fontScaleMultiplier;
                #endregion

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = currentRune.value <= 0xFFFF && currentRune.IsWhiteSpace();

                // Handle Mono Spacing
                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (layoutConfig.m_monoSpacing != 0)
                {
                    monoAdvance =
                        (layoutConfig.m_monoSpacing / 2 - (glyphWidth / 2 + x_bearing) * currentElementScale);  // * (1 - charWidthAdjDelta);
                    layoutConfig.m_xAdvance += monoAdvance;
                }
                #endregion

                // Set Padding based on selected font style
                #region Handle Style Padding
                float boldSpacingAdjustment = 0;
                //if bold is requested and current font is not bold (=it has not been found), then simulate bold
                bool simulateBold = (layoutConfig.fontWeight >= FontWeight.Bold && currentFontAssetRef.weight < FontWeight.Bold);
                if (simulateBold)
                {
                    //Debug.Log($"Simulate Bold {currentFontAssetRef.weight} {(int)FontWeight.Bold}");
                    boldSpacingAdjustment = 7; //this is not a property of font so might as well just set it here
                }
                #endregion Handle Style Padding

                var renderGlyphOld = new RenderGlyphOld();
                renderGlyphOld.glyphID = glyphID;

                var renderGlyph = new RenderGlyph();
                renderGlyph.arrayIndex = (uint)k;
                renderGlyph.glyphEntryId = glyphID; 

                // Determine the position of the vertices of the Character or Sprite.
                #region Calculate Vertices Position

                // top left is used to position the bottom left and top right
                float2 topLeft;
                topLeft.x = layoutConfig.m_xAdvance + (x_bearing * layoutConfig.m_fxScale - padding + glyphOTF.xOffset) * currentElementScale;
                topLeft.y = baselineOffset + (y_bearing + padding + glyphOTF.yOffset) * currentElementScale + layoutConfig.m_baselineOffset + m_subAndSupscriptOffset;

                float2 bottomLeft;
                bottomLeft.x = topLeft.x;
                bottomLeft.y = topLeft.y - ((glyphHeight + padding * 2) * currentElementScale);

                float2 topRight;
                topRight.x = bottomLeft.x + (glyphWidth * layoutConfig.m_fxScale + padding * 2) * currentElementScale;
                topRight.y = topLeft.y;

                float2 bottomRight;
                bottomRight.x = topRight.x;
                bottomRight.y = bottomLeft.y;
                #endregion

                // We don't set up UVA here, as that is the atlas texture coordinates.
                #region Setup UVB
                //Setup UV2 based on Character Mapping Options Selected
                //m_horizontalMapping case TextureMappingOptions.Character
                float2 blUVC, tlUVC, trUVC, brUVC;
                blUVC.x = 0;
                tlUVC.x = 0;
                trUVC.x = 1;
                brUVC.x = 1;

                //m_verticalMapping case case TextureMappingOptions.Character
                blUVC.y = 0;
                tlUVC.y = 1;
                trUVC.y = 1;
                brUVC.y = 0;

                renderGlyphOld.blUVB = blUVC;
                renderGlyphOld.tlUVB = tlUVC;
                renderGlyphOld.trUVB = trUVC;
                renderGlyphOld.brUVB = brUVC;

                renderGlyph.blUVB = blUVC;
                renderGlyph.tlUVB = tlUVC;
                renderGlyph.trUVB = trUVC;
                renderGlyph.brUVB = brUVC;
                #endregion

                #region Setup Color
                if (layoutConfig.useGradient) //&& !isColorGlyph)
                {
                    var gradient = layoutConfig.m_gradient;
                    renderGlyphOld.blColor = gradient.bottomLeft;
                    renderGlyphOld.tlColor = gradient.topLeft;
                    renderGlyphOld.trColor = gradient.topRight;
                    renderGlyphOld.brColor = gradient.bottomRight;

                    renderGlyph.blColor = GetColorAsHDRHalf4(gradient.bottomLeft);
                    renderGlyph.tlColor = GetColorAsHDRHalf4(gradient.topLeft);
                    renderGlyph.trColor = GetColorAsHDRHalf4(gradient.topRight);
                    renderGlyph.brColor = GetColorAsHDRHalf4(gradient.bottomRight);
                }
                else
                {
                    renderGlyphOld.blColor = layoutConfig.m_htmlColor;
                    renderGlyphOld.tlColor = layoutConfig.m_htmlColor;
                    renderGlyphOld.trColor = layoutConfig.m_htmlColor;
                    renderGlyphOld.brColor = layoutConfig.m_htmlColor;

                    var m_htmlColor = GetColorAsHDRHalf4(layoutConfig.m_htmlColor);
                    renderGlyph.blColor = m_htmlColor;
                    renderGlyph.tlColor = m_htmlColor;
                    renderGlyph.trColor = m_htmlColor;
                    renderGlyph.brColor = m_htmlColor;
                }
                #endregion

                #region Pack Scale into renderGlyph.scale
                var scale = layoutConfig.m_currentFontSize;
                if (simulateBold)
                    scale *= -1;

                renderGlyphOld.scale = scale;
                renderGlyph.scale = scale;
                #endregion

                // Check if we need to Shear the rectangles for Italic styles
                #region Handle Italic & Shearing
                float bottomShear = 0f;
                //if italic is requested and current font is not italic (=it has not been found), then simulate italic
                bool simulateItalic = (layoutConfig.m_fontStyles & FontStyles.Italic) == FontStyles.Italic && !currentFontAssetRef.isItalic;
                if (simulateItalic)
                {
                    //Debug.Log($"Simulate Italic {currentFontAssetRef.isItalic}");
                    // Shift Top vertices forward by half (Shear Value * height of character) and Bottom vertices back by same amount.
                    var italicsStyleSlant = 35; //this is not a property of font so might as well just set it here
                    float shear_value = italicsStyleSlant * 0.01f;
                    float midPoint = ((currentFont.capHeight - (currentFont.baseLine + layoutConfig.m_baselineOffset + m_subAndSupscriptOffset)) / 2) * fontScaleMultiplier;
                    float topShear = shear_value * ((y_bearing + padding - midPoint) * currentElementScale);
                    bottomShear = shear_value * ((y_bearing - glyphHeight - padding - midPoint) * currentElementScale);

                    topLeft.x += topShear;
                    bottomLeft.x += bottomShear;
                    topRight.x += topShear;
                    bottomRight.x += bottomShear;

                    renderGlyphOld.shear = topLeft.x - bottomLeft.x;
                }
                #endregion Handle Italics & Shearing

                // Needs to be done before rotation as it affects the position of the vertices and the old glyph can't handle that
#region Store vertex information for the character or sprite of the old glyphs.
                renderGlyphOld.trPosition = topRight;
                renderGlyphOld.blPosition = bottomLeft;
#endregion
                
                // Handle Character FX Rotation
                #region Handle Character FX Rotation

                float rotation = math.radians(layoutConfig.m_fxRotationAngleCCW_degree);
                renderGlyphOld.rotationCCW = rotation;
                if (math.abs(rotation) > 0.0001f)
                {
                    float2 pivot = (topLeft + bottomRight) * 0.5f;
                    math.sincos(rotation, out float sinRotation, out float cosRotation);

                    topLeft = RotatePoint(topLeft, pivot, sinRotation, cosRotation);
                    bottomLeft = RotatePoint(bottomLeft, pivot, sinRotation, cosRotation);
                    topRight = RotatePoint(topRight, pivot, sinRotation, cosRotation);
                    bottomRight = RotatePoint(bottomRight, pivot, sinRotation, cosRotation);
                }
                #endregion

                #region Store vertex information for the character or sprite.
                
                renderGlyph.trPosition = topRight;
                renderGlyph.tlPosition = topLeft;
                renderGlyph.blPosition = bottomLeft;
                renderGlyph.brPosition = bottomRight;
                if (Hint.Likely(currentRune.value != 10)) //do not render LF 
                {
                    renderGlyphsOld.Add(renderGlyphOld);
                    renderGlyphs.Add(renderGlyph);
                }
                #endregion

                // Compute text metrics
                #region Compute Ascender & Descender values
                // Element Ascender in line space
                float elementAscender = elementAscentLine * currentElementScale + layoutConfig.m_baselineOffset + m_subAndSupscriptOffset;

                // Element Descender in line space
                float elementDescender = elementDescentLine * currentElementScale + layoutConfig.m_baselineOffset + m_subAndSupscriptOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                // Max line ascender and descender in line space
                if (isLineStart || isWhiteSpace == false)
                {
                    // Special handling for Superscript and Subscript where we use the unadjusted line ascender and descender
                    if (m_subAndSupscriptOffset != 0) //To-Do: review (also voffset affecting m_baselineOffset), effect not clear. 
                    {
                        adjustedAscender = math.max((elementAscender - m_subAndSupscriptOffset) / fontScaleMultiplier, adjustedAscender);
                        adjustedDescender = math.min((elementDescender - m_subAndSupscriptOffset) / fontScaleMultiplier, adjustedDescender);
                    }
                    maxLineAscender = math.max(adjustedAscender, maxLineAscender);
                    maxLineDescender = math.min(adjustedDescender, maxLineDescender);
                }
                #endregion

                #region XAdvance, Tabulation & Stops
                if (currentRune.value == 9)
                {
                    float tabSize = currentFont.TabAdvance() * currentElementScale;
                    float tabs = math.ceil(layoutConfig.m_xAdvance / tabSize) * tabSize;
                    layoutConfig.m_xAdvance = tabs > layoutConfig.m_xAdvance ? tabs : layoutConfig.m_xAdvance + tabSize;
                }
                else if (layoutConfig.m_monoSpacing != 0)
                {
                    float monoAdjustment = layoutConfig.m_monoSpacing - monoAdvance;
                    layoutConfig.m_xAdvance += (monoAdjustment + layoutConfig.m_cSpacing);
                    if (isWhiteSpace || currentRune.value == 0x200B)
                        layoutConfig.m_xAdvance += textBaseConfiguration.wordSpacing * currentEmScale;
                }
                else
                {
                    layoutConfig.m_xAdvance += (glyphOTF.xAdvance * layoutConfig.m_fxScale) * currentElementScale +
                                boldSpacingAdjustment * currentEmScale + layoutConfig.m_cSpacing;

                    if (isWhiteSpace || currentRune.value == 0x200B)
                        layoutConfig.m_xAdvance += textBaseConfiguration.wordSpacing * currentEmScale;
                }
                #endregion XAdvance, Tabulation & Stops

                #region Check for Line Feed and Last Character
                if (isLineStart)
                    isLineStart = false;
                currentLineHeight = (currentFont.fontExtents.ascender - currentFont.fontExtents.descender) * baseScale;
                ascentLineDelta = maxLineAscender - currentFont.fontExtents.ascender * baseScale;
                decentLineDelta = currentFont.fontExtents.descender * baseScale - maxLineDescender;
                //if (currentRune.value == 10 || currentRune.value == 11 || currentRune.value == 0x03 || currentRune.value == 0x2028 ||
                //    currentRune.value == 0x2029 || textConfiguration.m_characterCount == calliString.Length - 1)
                if (currentRune.value == 10)
                {
                    var renderGlyphsOldLine = renderGlyphsOld.AsNativeArray().GetSubArray(startOfLineGlyphIndex, renderGlyphsOld.Length - startOfLineGlyphIndex);
                    var renderGlyphsLine = renderGlyphs.AsNativeArray().GetSubArray(startOfLineGlyphIndex, renderGlyphsOld.Length - startOfLineGlyphIndex);
                    var overrideMode = layoutConfig.m_lineJustification;
                    if (overrideMode == HorizontalAlignmentOptions.Justified)
                    {
                        // Don't perform justified spacing for the last line in the paragraph.
                        overrideMode = HorizontalAlignmentOptions.Left;
                    }
                    ApplyHorizontalAlignmentToGlyphs(ref renderGlyphsOldLine,
                                                     ref renderGlyphsLine,
                                                     ref characterGlyphIndicesWithPreceedingSpacesInLine,
                                                     textBaseConfiguration.maxLineWidth,
                                                     overrideMode);
                    startOfLineGlyphIndex = oldRenderGlyphs.Length;                  
                    if (!isFirstLine)
                    {
                        accumulatedVerticalOffset += currentLineHeight + ascentLineDelta;
                        if (lastCommittedStartOfLineGlyphIndex != startOfLineGlyphIndex)
                        {
                            ApplyVerticalOffsetToGlyphs(ref renderGlyphsOldLine, ref renderGlyphsLine, accumulatedVerticalOffset);
                            lastCommittedStartOfLineGlyphIndex = startOfLineGlyphIndex;
                        }
                    }
                    accumulatedVerticalOffset += decentLineDelta;
                    //apply user configurable line and paragraph spacing
                    accumulatedVerticalOffset +=
                        (textBaseConfiguration.lineSpacing + (currentRune.value == 10 || currentRune.value == 0x2029 ? textBaseConfiguration.paragraphSpacing : 0)) * currentEmScale;

                    //reset line status
                    maxLineAscender = float.MinValue;
                    maxLineDescender = float.MaxValue;

                    isFirstLine = false;
                    isLineStart = true;
                    bottomAnchor = GetBottomAnchorForConfig(ref currentFont, textBaseConfiguration.verticalAlignment, baseScale);

                    layoutConfig.m_xAdvance = layoutConfig.m_tagIndent;
                    previousRune = currentRune;
                    continue;
                }
                #endregion

                #region Word Wrapping
                // Handle word wrap
                if (textBaseConfiguration.maxLineWidth < float.MaxValue &&
                    textBaseConfiguration.maxLineWidth > 0 &&
                    layoutConfig.m_xAdvance > textBaseConfiguration.maxLineWidth)
                {
                    bool dropSpace = false;

                    if (currentRune.value == 32 && previousRune.value != 32)
                    {
                        // What pushed us past the line width was a space character.
                        // The previous character was not a space, and we don't
                        // want to render this character at the start of the next line.
                        // We drop this space character instead and allow the next
                        // character to line-wrap, space or not.
                        dropSpace = true;
                    }

                    var yOffsetChange = 0f;  //font.lineHeight * currentElementScale;
                    // TODO this line should be later replaced with renderGlyphs
                    var xOffsetChange = renderGlyphsOld[lastWordStartCharacterGlyphIndex].blPosition.x - bottomShear - layoutConfig.m_tagIndent;
                    if (xOffsetChange > 0 && !dropSpace)  // Always allow one visible character
                    {
                        // Finish line based on alignment
                        var renderGlyphsOldLine = renderGlyphsOld.AsNativeArray().GetSubArray(startOfLineGlyphIndex, lastWordStartCharacterGlyphIndex - startOfLineGlyphIndex);
                        var renderGlyphsLine = renderGlyphs.AsNativeArray().GetSubArray(startOfLineGlyphIndex, lastWordStartCharacterGlyphIndex - startOfLineGlyphIndex);
                        ApplyHorizontalAlignmentToGlyphs(ref renderGlyphsOldLine,
                                                         ref renderGlyphsLine,
                                                         ref characterGlyphIndicesWithPreceedingSpacesInLine,
                                                         textBaseConfiguration.maxLineWidth,
                                                         layoutConfig.m_lineJustification);

                        if (!isFirstLine)
                        {
                            accumulatedVerticalOffset += currentLineHeight + ascentLineDelta;
                            ApplyVerticalOffsetToGlyphs(ref renderGlyphsOldLine, ref renderGlyphsLine, accumulatedVerticalOffset);
                            lastCommittedStartOfLineGlyphIndex = startOfLineGlyphIndex;
                        }
                        accumulatedVerticalOffset += decentLineDelta;  // Todo: Delta should be computed per glyph
                        //apply user configurable line and paragraph spacing
                        accumulatedVerticalOffset += textBaseConfiguration.lineSpacing * currentEmScale;

                        //reset line status
                        maxLineAscender = float.MinValue;
                        maxLineDescender = float.MaxValue;

                        startOfLineGlyphIndex = lastWordStartCharacterGlyphIndex;
                        isLineStart = true;
                        isFirstLine = false;

                        layoutConfig.m_xAdvance -= xOffsetChange;

                        // Adjust the vertices of the previous render glyphs in the word
                        ApplyOffsetChange(ref renderGlyphsOld, lastWordStartCharacterGlyphIndex, xOffsetChange, yOffsetChange);
                        ApplyOffsetChange(ref renderGlyphs, lastWordStartCharacterGlyphIndex, xOffsetChange, yOffsetChange);
                    }
                }
                //Detect start of word
                if (currentRune.value == 32 ||  //Space
                    currentRune.value == 9 ||  //Tab
                    currentRune.value == 45 ||  //Hyphen Minus
                    currentRune.value == 173 ||  //Soft hyphen
                    currentRune.value == 8203 ||  //Zero width space
                    currentRune.value == 8204 ||  //Zero width non-joiner
                    currentRune.value == 8205)  //Zero width joiner
                {
                    lastWordStartCharacterGlyphIndex = renderGlyphsOld.Length;
                }
                #endregion
                previousRune = currentRune;
            }

            var finalRenderGlyphsOldLine = renderGlyphsOld.AsNativeArray().GetSubArray(startOfLineGlyphIndex, renderGlyphsOld.Length - startOfLineGlyphIndex);
            var finalRenderGlyphsLine = renderGlyphs.AsNativeArray().GetSubArray(startOfLineGlyphIndex, renderGlyphsOld.Length - startOfLineGlyphIndex);
            {
                var overrideMode = layoutConfig.m_lineJustification;
                if (overrideMode == HorizontalAlignmentOptions.Justified)
                {
                    // Don't perform justified spacing for the last line.
                    overrideMode = HorizontalAlignmentOptions.Left;
                }
                ApplyHorizontalAlignmentToGlyphs(ref finalRenderGlyphsOldLine, ref finalRenderGlyphsLine, ref characterGlyphIndicesWithPreceedingSpacesInLine, textBaseConfiguration.maxLineWidth, overrideMode);
                if (!isFirstLine)
                {
                    accumulatedVerticalOffset += currentLineHeight;
                    ApplyVerticalOffsetToGlyphs(ref finalRenderGlyphsOldLine, ref finalRenderGlyphsLine, accumulatedVerticalOffset);
                }
            }
            isFirstLine = false;
            ApplyVerticalAlignmentToGlyphs(ref renderGlyphsOld, ref renderGlyphs, topAnchor, bottomAnchor, accumulatedVerticalOffset, textBaseConfiguration.verticalAlignment);
        }
        
        static float2 RotatePoint(float2 point, float2 pivot, float sin, float cos)
        {
            float2 translated = point - pivot;
            return new float2(
                translated.x * cos - translated.y * sin,
                translated.x * sin + translated.y * cos
            ) + pivot;
        }

        static float GetTopAnchorForConfig(ref Font font, VerticalAlignmentOptions verticalMode, float baseScale, float oldValue = float.PositiveInfinity)
        {
            bool replace = oldValue == float.PositiveInfinity;
            switch (verticalMode)
            {
                case VerticalAlignmentOptions.TopBase: return 0f;
                case VerticalAlignmentOptions.MiddleTopAscentToBottomDescent:
                case VerticalAlignmentOptions.TopAscent: return baseScale * math.max(font.fontExtents.ascender - font.baseLine, math.select(oldValue, float.NegativeInfinity, replace));
                case VerticalAlignmentOptions.TopDescent: return baseScale * math.min(font.fontExtents.descender - font.baseLine, oldValue);
                case VerticalAlignmentOptions.TopCap: return baseScale * math.max(font.capHeight - font.baseLine, math.select(oldValue, float.NegativeInfinity, replace));
                case VerticalAlignmentOptions.TopMean: return baseScale * math.max(font.xHeight - font.baseLine, math.select(oldValue, float.NegativeInfinity, replace));
                default: return 0f;
            }
        }

        static float GetBottomAnchorForConfig(ref Font font, VerticalAlignmentOptions verticalMode, float baseScale, float oldValue = float.PositiveInfinity)
        {
            bool replace = oldValue == float.PositiveInfinity;
            switch (verticalMode)
            {
                case VerticalAlignmentOptions.BottomBase: return 0f;
                case VerticalAlignmentOptions.BottomAscent: return baseScale * math.max(font.fontExtents.ascender - font.baseLine, math.select(oldValue, float.NegativeInfinity, replace));
                case VerticalAlignmentOptions.MiddleTopAscentToBottomDescent:
                case VerticalAlignmentOptions.BottomDescent: return baseScale * math.min(font.fontExtents.descender - font.baseLine, oldValue);
                case VerticalAlignmentOptions.BottomCap: return baseScale * math.max(font.capHeight - font.baseLine, math.select(oldValue, float.NegativeInfinity, replace));
                case VerticalAlignmentOptions.BottomMean: return baseScale * math.max(font.xHeight - font.baseLine, math.select(oldValue, float.NegativeInfinity, replace));
                default: return 0f;
            }
        }

        static unsafe void ApplyHorizontalAlignmentToGlyphs(ref NativeArray<RenderGlyphOld> oldGlyphs,
                                                            ref NativeArray<RenderGlyph> renderGlyphs,
                                                            ref FixedList512Bytes<int> characterGlyphIndicesWithPreceedingSpacesInLine,
                                                            float width,
                                                            HorizontalAlignmentOptions alignMode)
        {
            if ((alignMode) == HorizontalAlignmentOptions.Left)
            {
                characterGlyphIndicesWithPreceedingSpacesInLine.Clear();
                return;
            }

            var oldGlyphsPtr = (RenderGlyphOld*)oldGlyphs.GetUnsafePtr();
            var glyphsPtr = (RenderGlyph*)renderGlyphs.GetUnsafePtr();
            if (alignMode == HorizontalAlignmentOptions.Center)
            {
                float oldOffset = oldGlyphsPtr[oldGlyphs.Length - 1].trPosition.x / 2f;
                for (int i = 0; i < oldGlyphs.Length; i++)
                {
                    oldGlyphsPtr[i].blPosition.x -= oldOffset;
                    oldGlyphsPtr[i].trPosition.x -= oldOffset;
                }
                float offset = glyphsPtr[renderGlyphs.Length - 1].trPosition.x / 2f;
                for (int i = 0; i < renderGlyphs.Length; i++)
                {
                    glyphsPtr[i].blPosition.x -= offset;
                    glyphsPtr[i].trPosition.x -= offset;
                    glyphsPtr[i].tlPosition.x -= offset;
                    glyphsPtr[i].brPosition.x -= offset;
                }
            }
            else if (alignMode == HorizontalAlignmentOptions.Right)
            {
                float oldOffset = oldGlyphsPtr[oldGlyphs.Length - 1].trPosition.x;
                for (int i = 0; i < oldGlyphs.Length; i++)
                {
                    oldGlyphsPtr[i].blPosition.x -= oldOffset;
                    oldGlyphsPtr[i].trPosition.x -= oldOffset;
                }
                float offset = glyphsPtr[renderGlyphs.Length - 1].trPosition.x;
                for (int i = 0; i < renderGlyphs.Length; i++)
                {
                    glyphsPtr[i].blPosition.x -= offset;
                    glyphsPtr[i].trPosition.x -= offset;
                    glyphsPtr[i].tlPosition.x -= offset;
                    glyphsPtr[i].brPosition.x -= offset;
                }
            }
            else  // Justified
            {
                float nudgePerSpace = (width - oldGlyphsPtr[oldGlyphs.Length - 1].trPosition.x) / characterGlyphIndicesWithPreceedingSpacesInLine.Length;
                float accumulatedOffset = 0f;
                int indexInIndices = 0;
                for (int i = 0; i < oldGlyphs.Length; i++)
                {
                    while (indexInIndices < characterGlyphIndicesWithPreceedingSpacesInLine.Length &&
                           characterGlyphIndicesWithPreceedingSpacesInLine[indexInIndices] == i)
                    {
                        accumulatedOffset += nudgePerSpace;
                        indexInIndices++;
                    }

                    oldGlyphsPtr[i].blPosition.x += accumulatedOffset;
                    oldGlyphsPtr[i].trPosition.x += accumulatedOffset;
                    
                    glyphsPtr[i].blPosition.x += accumulatedOffset;
                    glyphsPtr[i].trPosition.x += accumulatedOffset;
                    glyphsPtr[i].tlPosition.x += accumulatedOffset;
                    glyphsPtr[i].brPosition.x += accumulatedOffset;
                }
            }
            characterGlyphIndicesWithPreceedingSpacesInLine.Clear();
        }

        static unsafe void ApplyVerticalOffsetToGlyphs(ref NativeArray<RenderGlyphOld> oldGlyphs, ref NativeArray<RenderGlyph> glyphs, float accumulatedVerticalOffset)
        {
            for (int i = 0; i < oldGlyphs.Length; i++)
            {
                var glyph = oldGlyphs[i];
                glyph.blPosition.y -= accumulatedVerticalOffset;
                glyph.trPosition.y -= accumulatedVerticalOffset;
                oldGlyphs[i] = glyph;
            }
            for (int i = 0; i < glyphs.Length; i++)
            {
                var glyph = glyphs[i];
                glyph.blPosition.y -= accumulatedVerticalOffset;
                glyph.tlPosition.y -= accumulatedVerticalOffset;
                glyph.trPosition.y -= accumulatedVerticalOffset;
                glyph.brPosition.y -= accumulatedVerticalOffset;
                glyphs[i] = glyph;
            }
        }

        static unsafe void ApplyVerticalAlignmentToGlyphs(ref DynamicBuffer<RenderGlyphOld> oldGlyphs,
                                                          ref DynamicBuffer<RenderGlyph> glyphs,
        {
            var glyphPtr = (RenderGlyphOld*)glyphs.GetUnsafePtr();
            for (int i = lastWordStartCharacterGlyphIndex, ii= glyphs.Length; i < ii; i++)
            {
                glyphPtr[i].blPosition.y -= yOffsetChange;
                glyphPtr[i].blPosition.x -= xOffsetChange;
                glyphPtr[i].trPosition.y -= yOffsetChange;
                glyphPtr[i].trPosition.x -= xOffsetChange;
            }
        }

        static unsafe void ApplyVerticalAlignmentToGlyphs(ref DynamicBuffer<RenderGlyphOld> glyphs,
                                                          float topAnchor,
                                                          float bottomAnchor,
                                                          float accumulatedVerticalOffset,
                                                          VerticalAlignmentOptions alignMode)
        {
            var oldGlyphsPtr = (RenderGlyphOld*)oldGlyphs.GetUnsafePtr();
            var glyphsPtr = (RenderGlyph*)glyphs.GetUnsafePtr();
            switch (alignMode)
            {
                case VerticalAlignmentOptions.TopBase:
                    return;
                case VerticalAlignmentOptions.TopAscent:
                case VerticalAlignmentOptions.TopDescent:
                case VerticalAlignmentOptions.TopCap:
                case VerticalAlignmentOptions.TopMean:
                    {
                        // Positions were calculated relative to the baseline.
                        // Shift everything down so that y = 0 is on the target line.
                        for (int i = 0; i < oldGlyphs.Length; i++)
                        {
                            oldGlyphsPtr[i].blPosition.y -= topAnchor;
                            oldGlyphsPtr[i].trPosition.y -= topAnchor;
                        }
                        for (int i = 0; i < glyphs.Length; i++)
                        {
                            glyphsPtr[i].blPosition.y -= topAnchor;
                            glyphsPtr[i].tlPosition.y -= topAnchor;
                            glyphsPtr[i].trPosition.y -= topAnchor;
                            glyphsPtr[i].brPosition.y -= topAnchor;
                        }
                        break;
                    }
                case VerticalAlignmentOptions.BottomBase:
                case VerticalAlignmentOptions.BottomAscent:
                case VerticalAlignmentOptions.BottomDescent:
                case VerticalAlignmentOptions.BottomCap:
                case VerticalAlignmentOptions.BottomMean:
                    {
                        float offset = accumulatedVerticalOffset - bottomAnchor;
                        for (int i = 0; i < oldGlyphs.Length; i++)
                        {
                            oldGlyphsPtr[i].blPosition.y += offset;
                            oldGlyphsPtr[i].trPosition.y += offset;
                        }
                        for (int i = 0; i < glyphs.Length; i++)
                        {
                            glyphsPtr[i].blPosition.y += offset;
                            glyphsPtr[i].tlPosition.y += offset;
                            glyphsPtr[i].trPosition.y += offset;
                            glyphsPtr[i].brPosition.y += offset;
                        }
                        break;
                    }
                case VerticalAlignmentOptions.MiddleTopAscentToBottomDescent:
                    {
                        float fullHeight = accumulatedVerticalOffset - bottomAnchor + topAnchor;
                        float offset = fullHeight / 2f;
                        for (int i = 0; i < oldGlyphs.Length; i++)
                        {
                            oldGlyphsPtr[i].blPosition.y += offset;
                            oldGlyphsPtr[i].trPosition.y += offset;
                        }
                        for (int i = 0; i < glyphs.Length; i++)
                        {
                            glyphsPtr[i].blPosition.y += offset;
                            glyphsPtr[i].tlPosition.y += offset;
                            glyphsPtr[i].trPosition.y += offset;
                            glyphsPtr[i].brPosition.y += offset;
                        }
                        break;
                    }
            }
        }
        static unsafe void ApplyHorizontalAlignmentToGlyphs(ref NativeArray<RenderGlyph> glyphs,
                                                            ref FixedList512Bytes<int> characterGlyphIndicesWithPreceedingSpacesInLine,
                                                            float width,
                                                            HorizontalAlignmentOptions alignMode)
        {
            if ((alignMode) == HorizontalAlignmentOptions.Left)
            {
                characterGlyphIndicesWithPreceedingSpacesInLine.Clear();
                return;
            }

            var glyphsPtr = (RenderGlyphOld*)glyphs.GetUnsafePtr();
            if ((alignMode) == HorizontalAlignmentOptions.Center)
            {
                float offset = glyphsPtr[glyphs.Length - 1].trPosition.x / 2f;
                for (int i = 0; i < glyphs.Length; i++)
                {
                    glyphsPtr[i].blPosition.x -= offset;
                    glyphsPtr[i].trPosition.x -= offset;
                }
            }
            else if ((alignMode) == HorizontalAlignmentOptions.Right)
            {
                float offset = glyphsPtr[glyphs.Length - 1].trPosition.x;
                for (int i = 0; i < glyphs.Length; i++)
                {
                    glyphsPtr[i].blPosition.x -= offset;
                    glyphsPtr[i].trPosition.x -= offset;
                }
            }
            else  // Justified
            {
                float nudgePerSpace = (width - glyphsPtr[glyphs.Length - 1].trPosition.x) / characterGlyphIndicesWithPreceedingSpacesInLine.Length;
                float accumulatedOffset = 0f;
                int indexInIndices = 0;
                for (int i = 0; i < glyphs.Length; i++)
                {
                    while (indexInIndices < characterGlyphIndicesWithPreceedingSpacesInLine.Length &&
                           characterGlyphIndicesWithPreceedingSpacesInLine[indexInIndices] == i)
                    {
                        accumulatedOffset += nudgePerSpace;
                        indexInIndices++;
                    }

                    glyphsPtr[i].blPosition.x += accumulatedOffset;
                    glyphsPtr[i].trPosition.x += accumulatedOffset;
                }
            }
            characterGlyphIndicesWithPreceedingSpacesInLine.Clear();
        }
        static unsafe void ApplyOffsetChange(ref DynamicBuffer<RenderGlyphOld> glyphs, int lastWordStartCharacterGlyphIndex, float xOffsetChange, float yOffsetChange)
        {
            var glyphPtr = (RenderGlyphOld*)glyphs.GetUnsafePtr();
            for (int i = lastWordStartCharacterGlyphIndex, ii= glyphs.Length; i < ii; i++)
            {
                glyphPtr[i].blPosition.y -= yOffsetChange;
                glyphPtr[i].blPosition.x -= xOffsetChange;
                glyphPtr[i].trPosition.y -= yOffsetChange;
                glyphPtr[i].trPosition.x -= xOffsetChange;
            }
        }
        static unsafe void ApplyOffsetChange(ref DynamicBuffer<RenderGlyph> glyphs, int lastWordStartCharacterGlyphIndex, float xOffsetChange, float yOffsetChange)
        {
            var glyphPtr = (RenderGlyph*)glyphs.GetUnsafePtr();
            for (int i = lastWordStartCharacterGlyphIndex, ii = glyphs.Length; i < ii; i++)
            {
                glyphPtr[i].blPosition.y -= yOffsetChange;
                glyphPtr[i].blPosition.x -= xOffsetChange;
                glyphPtr[i].tlPosition.y -= yOffsetChange;
                glyphPtr[i].tlPosition.x -= xOffsetChange;
                glyphPtr[i].trPosition.y -= yOffsetChange;
                glyphPtr[i].trPosition.x -= xOffsetChange;
                glyphPtr[i].brPosition.y -= yOffsetChange;
                glyphPtr[i].brPosition.x -= xOffsetChange; 
            }
        }

        static half4 GetColorAsHDRHalf4(Color32 c)
        {
            return new half4(new half(c.r / 255f), new half(c.g / 255f), new half(c.b / 255f), new half(c.a / 255f));
        }
    }
}