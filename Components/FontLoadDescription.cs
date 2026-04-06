using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TextMeshDOTS
{
    [Serializable]
    [InternalBufferCapacity(0)]
    public struct FontLoadDescription : IEquatable<FontLoadDescription>, IBufferElementData
    {
        public FixedString512Bytes filePath;
        public bool streamingAssetLocationValidated;
        public bool isSystemFont;
        public int faceIndexInFile; 

        //face Information
        public FixedString128Bytes fontFamily;
        public FixedString128Bytes fontSubFamily;
        public FixedString128Bytes typographicFamily;
        public FixedString128Bytes typographicSubfamily;
        public float defaultWeight;
        public float defaultWidth;
        public bool isItalic;
        public float slant;
        public readonly FontLookupKey fontLookupKey => new FontLookupKey(fontFamily, typographicFamily, defaultWeight, defaultWidth, isItalic, slant);

        public override bool Equals(object obj)
        {
            if (obj is FontLoadDescription item)
            {
                return Equals(item);
            }
            return false;
        }
        bool IEquatable<FontLoadDescription>.Equals(FontLoadDescription other)
        {
            return fontLookupKey == other.fontLookupKey;
        }
        public override int GetHashCode()
        {
             return fontLookupKey.GetHashCode();
        }

        public static bool operator ==(FontLoadDescription target, FontLoadDescription other) { return target.Equals(other); }
        public static bool operator !=(FontLoadDescription target, FontLoadDescription other) { return !target.Equals(other); }
        public override string ToString()
        {
            if (typographicFamily != "")
                return $"{fontFamily} - {fontSubFamily} (typographic: {typographicFamily} - {typographicSubfamily})";
            else
                return $"{fontFamily} - {fontSubFamily}";
        }
    }
}
