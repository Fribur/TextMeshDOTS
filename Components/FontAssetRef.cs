using System;
using Unity.Collections;
using Unity.Entities;

namespace TextMeshDOTS
{
    // Todo: This is used by FontRequest. Once we decide what to do with that, we can maybe internalize this.

    /// <summary>
    /// FontAssetRef is THE link between any fonts request and font entities, and consists of a hash representing the 
    /// font family, and variation axis used during typesetting such as weight ("normal", "bold", semibold"), 
    /// width ("condensed", normal"), and italic. Slant is ignored in such font matching (see GetHashcode) 
    /// because slant value cannot be "guessed" and requested by user during typesetting 
    /// </summary>
    [Serializable]
    public struct FontAssetRef : IEquatable<FontAssetRef>
    {
        //Font selection logic: https://www.high-logic.com/font-editor/fontcreator/tutorials/font-family-settings
        public int familyHash;    //default to typeographic family, and fall-back to family if it does not exist
        public FontWeight weight;
        public float width;
        public bool isItalic;
        public float slant;

        public FontAssetRef(FixedString128Bytes fontFamily, FixedString128Bytes typographicFamily, FontWeight fontWeight, float width, bool isItalic, float slant)
        {
            this.familyHash = typographicFamily.IsEmpty ? TextHelper.GetHashCodeCaseInSensitive(fontFamily) : TextHelper.GetHashCodeCaseInSensitive(typographicFamily);
            this.weight = fontWeight;
            this.width = width;
            this.isItalic = isItalic;
            this.slant = slant;
        }
        public FontAssetRef(int familyNameHashCode, FontWeight fontWeight, float fontWidth, FontStyles fontStyles)
        {
            familyHash = familyNameHashCode;
            weight = fontWeight;
            width = fontWidth;
            isItalic = (fontStyles & FontStyles.Italic) == FontStyles.Italic;
            slant = 0;
        }
        public FontAssetRef(int familyNameHashCode, FontWeight fontWeight, FontWidth fontWidth, FontStyles fontStyles)
        {

            familyHash = familyNameHashCode;
            weight = fontWeight;
            width = (float)fontWidth;
            isItalic = (fontStyles & FontStyles.Italic) == FontStyles.Italic;
            slant = 0;
            //adjust according to https://learn.microsoft.com/en-us/typography/opentype/spec/os2#uswidthclass
            switch (fontWidth)
            {
                case FontWidth.ExtraCondensed:
                    width = 62.5f;
                    break;
                case FontWidth.SemiCondensed:
                    width = 87.5f;
                    break;
                case FontWidth.SemiExpanded:
                    width = 112.5f;
                    break;
            }
        }

        public override bool Equals(object obj) => obj is FontAssetRef other && Equals(other);

        public bool Equals(FontAssetRef other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public static bool operator ==(FontAssetRef e1, FontAssetRef e2)
        {
            return e1.GetHashCode() == e2.GetHashCode();
        }
        public static bool operator !=(FontAssetRef e1, FontAssetRef e2)
        {
            return e1.GetHashCode() != e2.GetHashCode();
        }
        
        public override int GetHashCode()
        {
            int hashCode = 2055808453;
            hashCode = hashCode * -1521134295 + familyHash;
            hashCode = hashCode * -1521134295 + (int)weight;
            hashCode = hashCode * -1521134295 + width.GetHashCode();
            hashCode = hashCode * -1521134295 + isItalic.GetHashCode();
            //fonts are search at runtime via FontAssetRef match. As slant angle cannot be guessed, do not inlcude this in hash
            //hashCode = hashCode * -1521134295 + slant.GetHashCode();
            return hashCode;
        }
        public override string ToString()
        {
            return $"FamilyHash {familyHash} weigth {weight} width {width} isItalic {isItalic}";
        }
    }
}