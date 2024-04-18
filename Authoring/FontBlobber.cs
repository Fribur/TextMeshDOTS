using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TextCore.Text;

namespace Latios.Calligraphics.Authoring
{
    public static class FontBlobber
    {
        public static unsafe BlobAssetReference<FontBlob> BakeFont(FontAsset font)
        {
            font.material.SetFloat("_WeightNormal", font.regularStyleWeight);
            font.material.SetFloat("_WeightBold", font.boldStyleWeight);
            float materialPadding = font.material.GetPaddingForText(false, false);

            var          builder             = new BlobBuilder(Allocator.Temp);
            ref FontBlob fontBlobRoot        = ref builder.ConstructRoot<FontBlob>();
            fontBlobRoot.scale               = font.faceInfo.scale;
            fontBlobRoot.pointSize           = font.faceInfo.pointSize;
            fontBlobRoot.baseLine            = font.faceInfo.baseline;
            fontBlobRoot.ascentLine          = font.faceInfo.ascentLine;
            fontBlobRoot.descentLine         = font.faceInfo.descentLine;
            fontBlobRoot.lineHeight          = font.faceInfo.lineHeight;
            fontBlobRoot.subscriptOffset     = font.faceInfo.subscriptOffset;
            fontBlobRoot.subscriptSize       = font.faceInfo.subscriptSize;
            fontBlobRoot.superscriptOffset   = font.faceInfo.superscriptOffset;
            fontBlobRoot.superscriptSize     = font.faceInfo.superscriptSize;
            fontBlobRoot.capLine             = font.faceInfo.capLine;
            fontBlobRoot.regularStyleSpacing = font.regularStyleSpacing;
            fontBlobRoot.regularStyleWeight  = font.regularStyleWeight;
            fontBlobRoot.boldStyleSpacing    = font.boldStyleSpacing;
            fontBlobRoot.boldStyleWeight     = font.boldStyleWeight;
            fontBlobRoot.italicsStyleSlant   = font.italicStyleSlant;            
            fontBlobRoot.atlasWidth          = font.atlasWidth;
            fontBlobRoot.atlasHeight         = font.atlasHeight;
            fontBlobRoot.materialPadding     = materialPadding;


            var       adjustmentCacheBefore      = new NativeList<int2>(Allocator.TempJob);
            var       adjustmentCacheAfter       = new NativeList<int2>(Allocator.TempJob);
            var       glyphToCharacterMap        = new NativeHashMap<int, int>(font.characterTable.Count, Allocator.TempJob);
            var       glyphPairAdjustmentsSource = font.GetGlyphPairAdjustmentRecords();
            Span<int> hashCounts                 = stackalloc int[64];
            hashCounts.Clear();
            // Todo: Currently, we allocate a glyph per character and leave characters with null glyphs uninitialized.
            // We should rework that to only allocate glyphs to save memory.
            BlobBuilderArray<GlyphBlob>      glyphBuilder    = builder.Allocate(ref fontBlobRoot.characters, font.characterTable.Count);
            BlobBuilderArray<AdjustmentPair> adjustmentPairs = builder.Allocate(ref fontBlobRoot.adjustmentPairs, glyphPairAdjustmentsSource.Count);

            for (int i = 0; i < font.characterTable.Count; i++)
            {
                var c = font.characterTable[i];
                if (c.glyph != null)
                    glyphToCharacterMap.Add((int)c.glyph.index, i);
            }

            for (int i = 0; i < glyphPairAdjustmentsSource.Count; i++)
            {
                var src            = glyphPairAdjustmentsSource[i];
                if(!(glyphToCharacterMap.ContainsKey((int)src.firstAdjustmentRecord.glyphIndex) && glyphToCharacterMap.ContainsKey((int)src.secondAdjustmentRecord.glyphIndex)))
                    continue;

                adjustmentPairs[i] = new AdjustmentPair
                {
                    firstAdjustment = new GlyphAdjustment
                    {
                        xPlacement = src.firstAdjustmentRecord.glyphValueRecord.xPlacement,
                        yPlacement = src.firstAdjustmentRecord.glyphValueRecord.yPlacement,
                        xAdvance   = src.firstAdjustmentRecord.glyphValueRecord.xAdvance,
                        yAdvance   = src.firstAdjustmentRecord.glyphValueRecord.yAdvance,
                    },
                    secondAdjustment = new GlyphAdjustment
                    {
                        xPlacement = src.secondAdjustmentRecord.glyphValueRecord.xPlacement,
                        yPlacement = src.secondAdjustmentRecord.glyphValueRecord.yPlacement,
                        xAdvance   = src.secondAdjustmentRecord.glyphValueRecord.xAdvance,
                        yAdvance   = src.secondAdjustmentRecord.glyphValueRecord.yAdvance,
                    },
                    fontFeatureLookupFlags = src.featureLookupFlags,
                    firstGlyphIndex        = glyphToCharacterMap[(int)src.firstAdjustmentRecord.glyphIndex],
                    secondGlyphIndex       = glyphToCharacterMap[(int)src.secondAdjustmentRecord.glyphIndex]
                };
            }

            for (int i = 0; i < font.characterTable.Count; i++)
            {
                var character = font.characterTable[i];

                if (character.glyph != null)
                {
                    ref GlyphBlob glyphBlob = ref glyphBuilder[i];

                    glyphBlob.unicode            = character.unicode;
                    glyphBlob.glyphScale         = character.glyph.scale;
                    glyphBlob.glyphMetrics       = character.glyph.metrics;
                    glyphBlob.glyphRect          = character.glyph.glyphRect;

                    //Add kerning adjustments
                    adjustmentCacheBefore.Clear();
                    adjustmentCacheAfter.Clear();
                    for (int j = 0; j < adjustmentPairs.Length; j++)
                    {
                        ref var adj = ref adjustmentPairs[j];
                        if (adj.firstGlyphIndex == i)
                            adjustmentCacheAfter.Add(new int2(adj.secondGlyphIndex, j));
                        if (adj.secondGlyphIndex == i)
                            adjustmentCacheBefore.Add(new int2(adj.firstGlyphIndex, j));
                    }
                    adjustmentCacheBefore.Sort(new XSorter());
                    var bk = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.beforeKeys, adjustmentCacheBefore.Length);
                    var bv = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.beforeIndices, adjustmentCacheBefore.Length);
                    for (int j = 0; j < bk.Length; j++)
                    {
                        var d = adjustmentCacheBefore[j];
                        bk[j] = d.x;
                        bv[j] = d.y;
                    }
                    adjustmentCacheAfter.Sort(new XSorter());
                    var ak = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.afterKeys, adjustmentCacheAfter.Length);
                    var av = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.afterIndices, adjustmentCacheAfter.Length);
                    for (int j = 0; j < ak.Length; j++)
                    {
                        var d = adjustmentCacheAfter[j];
                        ak[j] = d.x;
                        av[j] = d.y;
                    }

                    hashCounts[BlobTextMeshGlyphExtensions.GetGlyphHash(glyphBlob.unicode)]++;
                }
            }

            var             hashes     = builder.Allocate(ref fontBlobRoot.glyphLookupMap, 64);
            Span<HashArray> hashArrays = stackalloc HashArray[64];
            for (int i = 0; i < hashes.Length; i++)
            {
                hashArrays[i] = new HashArray
                {
                    hashArray = (GlyphLookup*)builder.Allocate(ref hashes[i], hashCounts[i]).GetUnsafePtr()
                };
                hashCounts[i] = 0;
            }

            for (int i = 0; i < glyphBuilder.Length; i++)
            {
                if (glyphBuilder[i].unicode == 0) // Is this the right way to rule out null glyphs?
                    continue;
                var hash                                     = BlobTextMeshGlyphExtensions.GetGlyphHash(glyphBuilder[i].unicode);
                hashArrays[hash].hashArray[hashCounts[hash]] = new GlyphLookup { unicode = glyphBuilder[i].unicode, index = i };
                hashCounts[hash]++;
            }

            var result = builder.CreateBlobAssetReference<FontBlob>(Allocator.Persistent);
            builder.Dispose();
            adjustmentCacheBefore.Dispose();
            adjustmentCacheAfter.Dispose();
            glyphToCharacterMap.Dispose();

            fontBlobRoot = result.Value;

            return result;
        }
        unsafe struct HashArray
        {
            public GlyphLookup* hashArray;
        }
        struct XSorter : IComparer<int2>
        {
            public int Compare(int2 x, int2 y) => x.x.CompareTo(y.x);
        }
    }
}

