using System;
using UnityEngine;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.RichText;
using Unity.Collections;

namespace TextMeshDOTS
{
    internal struct OpenTypeFeatureConfig : IDisposable
    {
        //https://learn.microsoft.com/en-us/typography/opentype/spec/featurelist

        public NativeList<Feature> values;
        public int smallCapsStartID;
        public int subscriptStartID;
        public int superscriptStartID;
        public int fractionStartID;
        internal OpenTypeFeatureConfig(int size, Allocator allocator)
        {
            values = new NativeList<Feature>(size, allocator);
            smallCapsStartID = -1;
            subscriptStartID = -1;
            superscriptStartID = -1;
            fractionStartID = -1;
        }
        internal void FinalizeOpenTypeFeatures(int position)
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
        internal void Update(ref XMLTag tag, int position)
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
}