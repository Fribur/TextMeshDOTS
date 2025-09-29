using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.RichText;
using Unity.Collections;
using UnityEngine;

namespace TextMeshDOTS
{
    internal struct FontConfig
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

        internal bool GetFontIndex(ref FontTable fontTable)
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


        /// <summary>
        /// In case the XMLTag  causes a change of the required font by changing any of the parameters in FontAssetRef, this method 
        /// searches the index of that font (0=main entity, >0 AdditionalFontEntity) in the provided FontAssetArray 
        /// </summary>
        internal void GetCurrentFontIndex(ref XMLTag tag, ref FontTable fontTable, ref CalliString calliStringRaw)
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
                            m_fontFamilyHash = TextHelper.GetHashCodeCaseInSensitive(stringValue); 
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
}