using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TextMeshDOTS
{
    [Serializable]
    [InternalBufferCapacity(0)]
    public struct FontRequest : IEquatable<FontRequest>, IBufferElementData
    {
        [SerializeField] public FontAssetRef fontAssetRef; //weight, width, italic, slant fields are redundantly stored here, but facilitates lookup of font Entities
        public FixedString128Bytes fontAssetPath;
        public bool streamingAssetLocationValidated;
        public FixedString128Bytes fontFamily;
        public FixedString128Bytes fontSubFamily;
        public FixedString128Bytes typographicFamily;
        public FixedString128Bytes typographicSubfamily;
        public FontWeight weight;
        public float width;
        public bool isItalic;
        public int slant;
        public bool useSystemFont;

        public int samplingPointSizeSDF;
        public int samplingPointSizeBitmap;

        public override bool Equals(object obj) => obj is FontRequest other && Equals(other);

        public bool Equals(FontRequest other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public static bool operator ==(FontRequest e1, FontRequest e2)
        {
            return e1.GetHashCode() == e2.GetHashCode();
        }
        public static bool operator !=(FontRequest e1, FontRequest e2)
        {
            return e1.GetHashCode() != e2.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hashCode = 2055808453;
            var familyHash = typographicFamily == string.Empty ? TextHelper.GetHashCodeCaseInsensitive(fontFamily) : TextHelper.GetHashCodeCaseInsensitive(typographicFamily);
            hashCode = hashCode * -1521134295 + familyHash;
            hashCode = hashCode * -1521134295 + ((byte)weight).GetHashCode();
            hashCode = hashCode * -1521134295 + width.GetHashCode();
            hashCode = hashCode * -1521134295 + isItalic.GetHashCode();
            hashCode = hashCode * -1521134295 + slant;
            return hashCode;
        }
    }
}
