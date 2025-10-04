using System;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.RichText;
using Unity.Collections;

namespace TextMeshDOTS
{
    public partial struct ShapeSystem
    {
        partial struct ShapeJob
        {
            struct OpenTypeFeatureConfig : IDisposable
            {
                //https://learn.microsoft.com/en-us/typography/opentype/spec/featurelist

                public NativeList<Feature> values;
                public int smallCapsStartID;
                public int subscriptStartID;
                public int superscriptStartID;
                public int fractionStartID;
                public OpenTypeFeatureConfig(int size, Allocator allocator)
                {
                    values = new NativeList<Feature>(size, allocator);
                    smallCapsStartID = -1;
                    subscriptStartID = -1;
                    superscriptStartID = -1;
                    fractionStartID = -1;
                }
                public void FinalizeOpenTypeFeatures(int position)
                {
                    if (smallCapsStartID != -1)
                        values.Add(new Feature(Harfbuzz.HB_TAG('s', 'm', 'c', 'p'), 1, (uint)smallCapsStartID, (uint)position));
                    if (subscriptStartID != -1)
                        values.Add(new Feature(Harfbuzz.HB_TAG('s', 'u', 'b', 's'), 1, (uint)subscriptStartID, (uint)position));
                    if (superscriptStartID != -1)
                        values.Add(new Feature(Harfbuzz.HB_TAG('s', 'u', 'p', 's'), 1, (uint)superscriptStartID, (uint)position));
                    if (fractionStartID != -1)
                        values.Add(new Feature(Harfbuzz.HB_TAG('f', 'r', 'a', 'c'), 1, (uint)fractionStartID, (uint)position));
                }
                public void Update(ref XMLTag tag, int position)
                {
                    switch (tag.tagType)
                    {
                        case TagType.SmallCaps:
                            if (!tag.isClosing)
                            {
                                if (smallCapsStartID == -1)
                                    smallCapsStartID = position;
                            }
                            else
                            {
                                values.Add(new Feature(Harfbuzz.HB_TAG('s', 'm', 'c', 'p'), 1, (uint)smallCapsStartID, (uint)position));
                                smallCapsStartID = -1;
                            }
                            return;
                        case TagType.Subscript:
                            if (!tag.isClosing)
                            {
                                if (subscriptStartID == -1)
                                    subscriptStartID = position;
                            }
                            else
                            {
                                values.Add(new Feature(Harfbuzz.HB_TAG('s', 'u', 'b', 's'), 1, (uint)subscriptStartID, (uint)position));
                                subscriptStartID = -1;
                            }
                            return;
                        case TagType.Superscript:
                            if (!tag.isClosing)
                            {
                                if (superscriptStartID == -1)
                                    superscriptStartID = position;
                            }
                            else
                            {
                                values.Add(new Feature(Harfbuzz.HB_TAG('s', 'u', 'p', 's'), 1, (uint)superscriptStartID, (uint)position));
                                superscriptStartID = -1;
                            }
                            return;
                        case TagType.Fraction:
                            if (!tag.isClosing)
                            {
                                if (fractionStartID == -1)
                                    fractionStartID = position;
                            }
                            else
                            {
                                values.Add(new Feature(Harfbuzz.HB_TAG('f', 'r', 'a', 'c'), 1, (uint)fractionStartID, (uint)position));
                                fractionStartID = -1;
                            }
                            return;
                    }

                }
                public void SetGlobalFeatures(in TextBaseConfiguration textBaseConfiguration, uint textLength)
                {
                    if ((textBaseConfiguration.fontStyles & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                        values.Add(new Feature(Harfbuzz.HB_TAG('s', 'm', 'c', 'p'), 1, 0, textLength));
                    if ((textBaseConfiguration.fontStyles & FontStyles.Subscript) == FontStyles.Subscript)
                        values.Add(new Feature(Harfbuzz.HB_TAG('s', 'u', 'b', 's'), 1, 0, textLength));
                    if ((textBaseConfiguration.fontStyles & FontStyles.Superscript) == FontStyles.Superscript)
                        values.Add(new Feature(Harfbuzz.HB_TAG('s', 'u', 'p', 's'), 1, 0, textLength));
                    if ((textBaseConfiguration.fontStyles & FontStyles.Fraction) == FontStyles.Fraction)
                        values.Add(new Feature(Harfbuzz.HB_TAG('f', 'r', 'a', 'c'), 1, 0, textLength));
                }
                public void Clear()
                {
                    values.Clear();
                    smallCapsStartID = -1;
                    subscriptStartID = -1;
                    superscriptStartID = -1;
                    fractionStartID = -1;
                }

                public void Dispose()
                {
                    if (values.IsCreated) values.Dispose();
                }
            }

            struct FontConfig
            {
                public int m_fontMaterialIndex;

                public int m_fontFamilyHash;
                public FixedStack512Bytes<int> m_fontFamilyHashStack;

                public FontWeight m_fontWeight;
                public FixedStack512Bytes<FontWeight> m_fontWeightStack;

                public float m_fontWidth;
                public FixedStack512Bytes<float> m_fontWidthStack;

                public FontStyles m_fontStyles; //only used for italic state

                FontAssetRef FontAssetRef   // property
                {
                    get { return new FontAssetRef(m_fontFamilyHash, m_fontWeight, m_fontWidth, m_fontStyles); }
                }

                public void Reset(in TextBaseConfiguration textBaseConfiguration, ref FontTable fontTable)
                {
                    m_fontStyles = textBaseConfiguration.fontStyles;

                    m_fontFamilyHash = textBaseConfiguration.defaultFontFamilyHash;
                    m_fontFamilyHashStack.Clear();
                    m_fontFamilyHashStack.Add(m_fontFamilyHash);

                    m_fontWeight = textBaseConfiguration.fontWeight;
                    m_fontWeightStack.Clear();
                    m_fontWeightStack.Add(m_fontWeight);

                    m_fontWidth = textBaseConfiguration.fontWidth.Value();
                    m_fontWidthStack.Clear();
                    m_fontWidthStack.Add(m_fontWidth);

                    m_fontMaterialIndex = 0;
                    GetFontIndex(ref fontTable);
                }

                public bool GetFontIndex(ref FontTable fontTable)
                {
                    var fontIndex = fontTable.GetFontIndex(FontAssetRef);
                    if (fontIndex != -1)
                    {
                        m_fontMaterialIndex = fontIndex;
                        return true;
                    }
                    else
                    {
                        m_fontMaterialIndex = 0;
                        return false;
                    }
                }


                public void Update(ref XMLTag tag, ref FontTable fontTable, ref CalliString calliStringRaw)
                {
                    switch (tag.tagType)
                    {
                        case TagType.Italic:
                            if (!tag.isClosing)
                                m_fontStyles |= FontStyles.Italic;
                            else
                                m_fontStyles &= ~FontStyles.Italic;

                            GetFontIndex(ref fontTable);
                            return;
                        case TagType.Bold:
                            if (!tag.isClosing)
                            {
                                m_fontWeight = FontWeight.Bold;
                                m_fontWeightStack.Add(m_fontWeight);
                            }
                            else
                                m_fontWeight = m_fontWeightStack.RemoveExceptRoot();

                            GetFontIndex(ref fontTable);
                            return;
                        case TagType.FontWeight:
                            if (!tag.isClosing)
                            {
                                m_fontWeight = (FontWeight)tag.value.NumericalValue;
                                m_fontWeightStack.Add(m_fontWeight);
                            }
                            else
                                m_fontWeight = m_fontWeightStack.RemoveExceptRoot();

                            GetFontIndex(ref fontTable);
                            return;
                        case TagType.FontWidth:
                            if (!tag.isClosing)
                            {
                                m_fontWidth = tag.value.NumericalValue;
                                m_fontWidthStack.Add(m_fontWidth);
                            }
                            else
                                m_fontWidth = m_fontWidthStack.RemoveExceptRoot();

                            GetFontIndex(ref fontTable);
                            return;
                        case TagType.Font:
                            if (!tag.isClosing)
                            {
                                if (tag.value.stringValue == StringValue.Default)
                                    m_fontFamilyHash = m_fontFamilyHashStack[0];
                                else
                                {
                                    //fetch name of font from calliStringRaw Buffer
                                    //To-Do: better to store valueHash in tag struct
                                    FixedString128Bytes stringValue = default; //should not happen too often, so should be OK to allocate here
                                    calliStringRaw.GetSubString(ref stringValue, tag.value.valueStart, tag.value.valueLength);
                                    m_fontFamilyHash = TextHelper.GetHashCodeCaseInsensitive(stringValue);
                                    m_fontFamilyHashStack.Add(m_fontFamilyHash);
                                }

                                GetFontIndex(ref fontTable);
                                return;
                            }
                            else
                            {
                                m_fontFamilyHash = m_fontFamilyHashStack.RemoveExceptRoot();

                                GetFontIndex(ref fontTable);
                            }
                            return;
                    }
                }
            }

            // Use LayoutConfig to change case prior to hb-shape. Works only for latin text
            // Should this use cases really be in scope of TextMeshDOTS? 
            struct LayoutConfig
            {
                public FontStyles m_fontStyles;

                public LayoutConfig(in TextBaseConfiguration textBaseConfiguration)
                {
                    m_fontStyles = textBaseConfiguration.fontStyles;
                }
                public void Reset(in TextBaseConfiguration textBaseConfiguration)
                {
                    m_fontStyles = textBaseConfiguration.fontStyles;
                }
                public void Update(ref XMLTag tag)
                {
                    switch (tag.tagType)
                    {
                        case TagType.AllCaps:
                        case TagType.Uppercase:
                        {
                            if (tag.isClosing)
                                m_fontStyles &= ~FontStyles.UpperCase;
                            else
                                m_fontStyles |= FontStyles.UpperCase;
                        }
                        break;
                        case TagType.Lowercase:
                        {
                            if (tag.isClosing)
                                m_fontStyles &= ~FontStyles.LowerCase;
                            else
                                m_fontStyles |= FontStyles.LowerCase;
                        }
                        break;
                    }
                }
            }
        }
    }
}