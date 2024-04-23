using TextMeshDOTS.Rendering;
using Unity.Collections;
using Unity.Entities;

namespace TextMeshDOTS
{
    internal unsafe struct FontMaterialSet
    {
        FixedList4096Bytes<FontMaterial>            m_fontMaterialArray;
        FixedList512Bytes<byte>                     m_fontToEntityIndexArray;
        DynamicBuffer<FontMaterialSelectorForGlyph> m_selectorBuffer;
        bool                                        m_hasMultipleFonts;

        public ref FontBlob this[int index] => ref m_fontMaterialArray[index].font;

        public void WriteFontMaterialIndexForGlyph(int index)
        {
            if (!m_hasMultipleFonts)
                return;
            var remap                                                                 = m_fontToEntityIndexArray[index];
            m_selectorBuffer.Add(new FontMaterialSelectorForGlyph { fontMaterialIndex = remap });
        }

        public void Initialize(BlobAssetReference<FontBlob> singleFont)
        {
            m_hasMultipleFonts = false;
            m_fontMaterialArray.Clear();
            m_fontMaterialArray.Add(new FontMaterial(singleFont));
        }

        unsafe struct FontMaterial
        {
            FontBlob* m_fontBlobPtr;

            public ref FontBlob font => ref *m_fontBlobPtr;

            public FontMaterial(BlobAssetReference<FontBlob> blobRef)
            {
                m_fontBlobPtr = (FontBlob*)blobRef.GetUnsafePtr();
            }
        }
    }
}

