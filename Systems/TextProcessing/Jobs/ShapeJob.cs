using TextMeshDOTS.HarfBuzz;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Profiling;
using Buffer = TextMeshDOTS.HarfBuzz.Buffer;

namespace TextMeshDOTS
{
    [BurstCompile]
    internal partial struct ShapeJob : IJobChunk
    {
        [ReadOnly] public ProfilerMarker marker;
        [ReadOnly] public ProfilerMarker marker2;

        public BufferTypeHandle<GlyphOTF> glyphOTFHandle;

        [ReadOnly] internal FontTable fontTable;
        [ReadOnly] internal GlyphTable glyphTable;
        [ReadOnly] public ComponentTypeHandle<TextBaseConfiguration> textBaseConfigurationHandle;
        [ReadOnly] public BufferTypeHandle<CalliByte> calliByteHandle;
        [ReadOnly] public BufferTypeHandle<XMLTag> xmlTagHandle;
        public NativeStream.Writer missingGlyphsStream;

        public uint lastSystemVersion;

        UnsafeHashSet<GlyphTable.Key> chunkMissingGlyphsSet;

        [NativeSetThreadIndex]
        int threadIndex;

        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (!(chunk.DidChange(ref textBaseConfigurationHandle, lastSystemVersion) ||
                  chunk.DidChange(ref xmlTagHandle, lastSystemVersion)))
                return;

            if (!chunkMissingGlyphsSet.IsCreated)
                chunkMissingGlyphsSet = new UnsafeHashSet<GlyphTable.Key>(128, Allocator.Temp);
            chunkMissingGlyphsSet.Clear();

            missingGlyphsStream.BeginForEachIndex(unfilteredChunkIndex);
            //Debug.Log("Shape job");
            var calliBytesBuffers = chunk.GetBufferAccessor(ref calliByteHandle);
            var xmlTagBuffers = chunk.GetBufferAccessor(ref xmlTagHandle);
            var glyphOTFBuffers = chunk.GetBufferAccessor(ref glyphOTFHandle);
            var textBaseConfigurations = chunk.GetNativeArray(ref textBaseConfigurationHandle);

            var language = new Language(Harfbuzz.HB_TAG('E', 'N', 'G', ' '));
            //var language = new Language(HB.HB_TAG('A', 'P', 'P', 'H'));
            var segmentProperties = new SegmentProperties(Direction.LTR, Script.LATIN, language);
            var buffer = new Buffer(true);
            var openTypeFeatures = new OpenTypeFeatureConfig(16, Allocator.Temp);            

            //shape plans can be cached..no use case found yet where there this makes a significant difference
            //var shaperList = HB.hb_shape_list_shapers();
            //var shapePlanCache = new NativeHashMap<FontAssetRef, ShapePlan>(16, Allocator.Temp);

            var cleanedString = new NativeText(1024, Allocator.Temp);
            LayoutConfig2 layoutConfig2 = default;
            FontConfig fontConfiguration = default;

            for (int indexInChunk = 0; indexInChunk < chunk.Count; indexInChunk++)
            {
                var xmlTagBuffer = xmlTagBuffers[indexInChunk];
                var glyphOTFs = glyphOTFBuffers[indexInChunk];
                var calliBytesBuffer = calliBytesBuffers[indexInChunk].Reinterpret<byte>();
                var textBaseConfiguration = textBaseConfigurations[indexInChunk];

                fontConfiguration.Reset(textBaseConfiguration, ref fontTable);
                layoutConfig2.Reset(textBaseConfiguration);
                glyphOTFs.Clear();
                var calliString = new CalliString(calliBytesBuffer);
                cleanedString.Capacity = calliString.Capacity;         

                if (xmlTagBuffer.Length == 0)
                    ShapeNoRichText(calliString, ref layoutConfig2, cleanedString, ref fontConfiguration, ref fontTable, ref openTypeFeatures, ref textBaseConfiguration, ref segmentProperties, ref buffer, ref glyphOTFs);
                else
                    ShapeRichText(calliString, ref layoutConfig2, cleanedString, ref fontConfiguration, ref fontTable, ref openTypeFeatures, ref textBaseConfiguration, ref segmentProperties, ref buffer, ref glyphOTFs, ref xmlTagBuffer);

                cleanedString.Clear();
            }           
            //add missing glyphs identifed in chunks processed by this thread to missingGlyphs
            missingGlyphsStream.EndForEachIndex();
            buffer.Dispose();
        }

        void AppendAndConvertCase(NativeText cleanedString, FontStyles fontStyles, ref Unicode.Rune currentRune)
        {
            if ((fontStyles & FontStyles.UpperCase) == FontStyles.UpperCase)
                cleanedString.Append(currentRune.ToUpper());
            else if ((fontStyles & FontStyles.LowerCase) == FontStyles.LowerCase)
                cleanedString.Append(currentRune.ToLower());
            else
                cleanedString.Append(currentRune);
        }
        void ShapeNoRichText(CalliString calliString, 
            ref LayoutConfig2 layoutConfig2, 
            NativeText cleanedString, 
            ref FontConfig fontConfiguration, 
            ref FontTable fontTable,
            ref OpenTypeFeatureConfig openTypeFeatures,
            ref TextBaseConfiguration textBaseConfiguration,
            ref SegmentProperties segmentProperties,
            ref Buffer buffer,
            ref DynamicBuffer<GlyphOTF> glyphOTFs)
        {
            var rawCharacters = calliString.GetEnumerator();
            int currentFontMaterialIndex = fontConfiguration.m_fontMaterialIndex;
            //copy text into buffer used for shaping, convert case while doing so
            while (rawCharacters.MoveNext())
            {
                var currentRune = rawCharacters.Current;
                AppendAndConvertCase(cleanedString, layoutConfig2.m_fontStyles, ref currentRune);
            }
            //find font Entity requested by combination of font family and style
            fontConfiguration.GetFontIndex(ref fontTable);
            openTypeFeatures.SetGlobalFeatures(textBaseConfiguration, (uint)cleanedString.Length);
            Shape(buffer, cleanedString, 0, cleanedString.Length, ref segmentProperties, ref fontTable, currentFontMaterialIndex, openTypeFeatures.values, glyphOTFs);            
        }

        void ShapeRichText(CalliString calliString,
          ref LayoutConfig2 layoutConfig2,
          NativeText cleanedString,
          ref FontConfig fontConfiguration,
          ref FontTable fontTable,
          ref OpenTypeFeatureConfig openTypeFeatures,
          ref TextBaseConfiguration textBaseConfiguration,
          ref SegmentProperties segmentProperties,
          ref Buffer buffer,
          ref DynamicBuffer<GlyphOTF> glyphOTFs,
          ref DynamicBuffer<XMLTag> xmlTagBuffer)
        {
            //text has richtext tags. Search segments where font, language, script and direction does does not change (To-Do: use ICU for that),
            //apply opentype features requested via richtext tags, and shape
            var rawCharacters = calliString.GetEnumerator();
            int currentFontMaterialIndex = fontConfiguration.m_fontMaterialIndex;
            int tagsCounter = 0;
            var nextTagPosition = tagsCounter < xmlTagBuffer.Length ? xmlTagBuffer[tagsCounter].startID : calliString.Length;
            

            //copy text into buffer used for shaping, convert case while doing so
            bool keepGoing;
            XMLTag currentTag;
            while (keepGoing = rawCharacters.MoveNext())
            {
                var currentRune = rawCharacters.Current;
                while (tagsCounter < xmlTagBuffer.Length && rawCharacters.NextRuneByteIndex > nextTagPosition)
                {
                    currentTag = xmlTagBuffer[tagsCounter];
                    rawCharacters.GotoByteIndex(currentTag.endID);              // go to ">'
                    keepGoing = rawCharacters.MoveNext();                       // go to char after '>'                        
                    layoutConfig2.Update(ref currentTag);
                    currentRune = rawCharacters.Current;
                    tagsCounter++;
                    nextTagPosition = tagsCounter < xmlTagBuffer.Length ? xmlTagBuffer[tagsCounter].startID : calliString.Length;
                    //continue;
                }
                if (!keepGoing)
                    continue;
                AppendAndConvertCase(cleanedString, layoutConfig2.m_fontStyles, ref currentRune);
            }

            var richTextStartID = 0;
            var cleanedEnd = 0;
            var cleanedStart = 0;
            tagsCounter = 0;
            while (cleanedStart < cleanedString.Length)
            {
                while (tagsCounter < xmlTagBuffer.Length && fontConfiguration.m_fontMaterialIndex == currentFontMaterialIndex)
                {
                    currentTag = xmlTagBuffer[tagsCounter];
                    var cleanedInterTagLength = (currentTag.startID - richTextStartID);
                    cleanedEnd += cleanedInterTagLength;
                    fontConfiguration.GetCurrentFontIndex(ref currentTag, ref fontTable, ref calliString);
                    openTypeFeatures.Update(ref currentTag, cleanedEnd);
                    tagsCounter++;
                    richTextStartID = currentTag.endID + 1;
                }
                openTypeFeatures.FinalizeOpenTypeFeatures(cleanedString.Length);
                openTypeFeatures.SetGlobalFeatures(textBaseConfiguration, (uint)cleanedString.Length);
                var cleanedSegmentLength = cleanedEnd - cleanedStart;
                Shape(buffer, cleanedString, cleanedStart, cleanedSegmentLength, ref segmentProperties, ref fontTable, currentFontMaterialIndex, openTypeFeatures.values, glyphOTFs);
                currentFontMaterialIndex = fontConfiguration.m_fontMaterialIndex;
                cleanedStart = cleanedEnd;
                if (tagsCounter == xmlTagBuffer.Length) //last loop in order to shape text between last tag and end of rich text buffer
                    cleanedEnd = cleanedString.Length;
            }
        }

        void Shape(Buffer buffer,
            //NativeArray<byte> text,
            NativeText text,
            int startIndex,
            int length,
            ref SegmentProperties segmentProperties, 
            ref FontTable fontTable,
            int fontMaterialIndex,
            NativeList<Feature> features,
            DynamicBuffer<GlyphOTF> glyphOTFs)
        {
            if (startIndex + length == text.Length && text[^1] == 0)
                length--; //last byte of CalliBytes buffer appears to be always '0', which should not be shaped. 
            buffer.AddText(text, (uint)startIndex, length);
            buffer.SetSegmentProperties(ref segmentProperties);

            //a number of white spaces are regretably not replaced by "space" (needs to be handled in GenerateGlyphJob)
            //https://github.com/harfbuzz/harfbuzz/commit/81ef4f407d9c7bd98cf62cef951dc538b13442eb#commitcomment-9469767
            buffer.BufferFlag = BufferFlag.REMOVE_DEFAULT_IGNORABLES | BufferFlag.BOT | BufferFlag.EOT;

            var fontAssetRef = fontTable.fontAssetRefs[fontMaterialIndex];
            var faceIndex = this.fontTable.fontAssetRefToFaceIndexMap[fontAssetRef];
            var face = this.fontTable.faces[faceIndex];
            var renderFormat = face.renderFormat;

            var font = this.fontTable.GetOrCreateFont(faceIndex, threadIndex);
            var samplingSize = FontTextureSize.Normal.GetSamplingSize();
            font.SetScale(samplingSize, samplingSize);

            //UnityEngine.Debug.Log($"fontEntity: {fontEntity.ToFixedString()}, from faceIndex: {fontTable.faceIndexToFontEntityMap[faceIndex].ToFixedString()}");
            //if (!shapePlanCache.TryGetValue(fontAssetRef, out var shapePlan))
            //{                        
            //    shapePlan = new ShapePlan(nativeFontPointer.face, ref segmentProperties, features, shaperList);
            //    shapePlanCache.Add(fontAssetRef, shapePlan);
            //}
            //marker.Begin();
            //shapePlan.Execute(font, buffer, features);
            //marker.End();

            marker.Begin();
            font.Shape(buffer, features);
            marker.End();

            var glyphInfos = buffer.GetGlyphInfosSpan();
            var glyphPositions = buffer.GetGlyphPositionsSpan();
            var capacity = glyphOTFs.Length + glyphInfos.Length;
            glyphOTFs.Capacity = capacity; //2x speedup compared to allocating for each element

            for (int i = 0, ii = glyphInfos.Length; i < ii; i++)
            {
                var glyphInfo = glyphInfos[i];
                var glyphPosition = glyphPositions[i];
                var codepoint = glyphInfo.codepoint;
                
                var glyphOTF = new GlyphOTF
                {
                    glyphKey = new GlyphTable.Key
                    {
                        faceIndex = faceIndex,
                        glyphIndex = (ushort)glyphInfo.codepoint,
                        format = renderFormat,
                        textureSize = FontTextureSize.Normal,
                        variableProfileIndex = 0
                    },
                    cluster =  glyphInfo.cluster,
                    xAdvance = glyphPosition.xAdvance,
                    yAdvance = glyphPosition.yAdvance,
                    xOffset = glyphPosition.xOffset,
                    yOffset = glyphPosition.yOffset,
                };
                if (!glyphTable.glyphHashToIdMap.ContainsKey(glyphOTF.glyphKey))
                {
                    // We use the hashset to avoid redundantly adding the same glyph for this chunk.
                    // The missingGlyphsStream may still have redundancies between chunks, but this reduces
                    // some of the work while still maintaining determinism.
                    if (chunkMissingGlyphsSet.Add(glyphOTF.glyphKey))
                        missingGlyphsStream.Write(glyphOTF.glyphKey);
                }
                glyphOTFs.Add(glyphOTF);
            }
            buffer.ClearContent();
            features.Clear();
        }
    }
}