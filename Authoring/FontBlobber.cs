using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace TextMeshDOTS.Authoring
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
            fontBlobRoot.name                = font.name;
            fontBlobRoot.scale               = font.faceInfo.scale;
            fontBlobRoot.pointSize           = font.faceInfo.pointSize;
            fontBlobRoot.baseLine            = font.faceInfo.baseline;
            fontBlobRoot.ascentLine          = font.faceInfo.ascentLine;
            fontBlobRoot.descentLine         = font.faceInfo.descentLine;
			fontBlobRoot.capLine             = font.faceInfo.capLine;
            fontBlobRoot.meanLine            = font.faceInfo.meanLine;
            fontBlobRoot.lineHeight          = font.faceInfo.lineHeight;
            fontBlobRoot.subscriptOffset     = font.faceInfo.subscriptOffset;
            fontBlobRoot.subscriptSize       = font.faceInfo.subscriptSize;
            fontBlobRoot.superscriptOffset   = font.faceInfo.superscriptOffset;
            fontBlobRoot.superscriptSize     = font.faceInfo.superscriptSize;
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
            var       glyphPairAdjustmentsSource = font.GetGlyphPairAdjustmentRecords();
            Span<int> hashCounts                 = stackalloc int[64];
            hashCounts.Clear();
            // Todo: Currently, we allocate a glyph per character and leave characters with null glyphs uninitialized.
            // We should rework that to only allocate glyphs to save memory.
            BlobBuilderArray<GlyphBlob>      glyphBuilder    = builder.Allocate(ref fontBlobRoot.characters, font.characterTable.Count);
            BlobBuilderArray<AdjustmentPair> adjustmentPairs = builder.Allocate(ref fontBlobRoot.adjustmentPairs, glyphPairAdjustmentsSource.Count);
            var characterTable = font.characterTable;            
            for (int i = 0; i < glyphPairAdjustmentsSource.Count; i++)
            {
                var kerningPair = glyphPairAdjustmentsSource[i];
                if (GlyphIndexToUnicode(kerningPair.firstAdjustmentRecord.glyphIndex, characterTable, out int firstUnicode) &&
                    GlyphIndexToUnicode(kerningPair.secondAdjustmentRecord.glyphIndex, characterTable, out int secondUnicode))                    
                {
                    adjustmentPairs[i] = new AdjustmentPair
                    {
                        firstAdjustment = new GlyphAdjustment
                        {
                            xPlacement = kerningPair.firstAdjustmentRecord.glyphValueRecord.xPlacement,
                            yPlacement = kerningPair.firstAdjustmentRecord.glyphValueRecord.yPlacement,
                            xAdvance   = kerningPair.firstAdjustmentRecord.glyphValueRecord.xAdvance,
                            yAdvance   = kerningPair.firstAdjustmentRecord.glyphValueRecord.yAdvance,
                        },
                        secondAdjustment = new GlyphAdjustment
                        {
                            xPlacement = kerningPair.secondAdjustmentRecord.glyphValueRecord.xPlacement,
                            yPlacement = kerningPair.secondAdjustmentRecord.glyphValueRecord.yPlacement,
                            xAdvance   = kerningPair.secondAdjustmentRecord.glyphValueRecord.xAdvance,
                            yAdvance   = kerningPair.secondAdjustmentRecord.glyphValueRecord.yAdvance,
                        },
                        fontFeatureLookupFlags = kerningPair.featureLookupFlags,
                        firstUnicode 		   = firstUnicode,
                        secondUnicode 		   = secondUnicode
                    };
                }
            }

            for (int i = 0; i < font.characterTable.Count; i++)
            {
                var character = font.characterTable[i];
                var glyph 	  = character.glyph;
                if (glyph == null)
                    continue;
                var unicode = math.asint(character.unicode);

                ref GlyphBlob glyphBlob = ref glyphBuilder[i];

                glyphBlob.unicode            = unicode;
                glyphBlob.glyphScale         = glyph.scale;
                glyphBlob.glyphMetrics       = glyph.metrics;
                glyphBlob.glyphRect          = glyph.glyphRect;

                //Add kerning adjustments
                adjustmentCacheBefore.Clear();
                adjustmentCacheAfter.Clear();
                for (int j = 0; j < adjustmentPairs.Length; j++)
                {
                    ref var adj = ref adjustmentPairs[j];
                    if (adj.firstUnicode == unicode)
                        adjustmentCacheAfter.Add(new int2(adj.secondUnicode, j));
                    if (adj.secondUnicode == unicode)
                        adjustmentCacheBefore.Add(new int2(adj.firstUnicode, j));
                }
                adjustmentCacheBefore.Sort(new XSorter());
                var bk = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.beforeKeys, adjustmentCacheBefore.Length);
                var bv = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.beforeIndices, adjustmentCacheBefore.Length);
                for (int j = 0; j < bk.Length; j++)
                {
                    var d = adjustmentCacheBefore[j];
                    bk[j] = d.x; //unicode
                    bv[j] = d.y;
                }
                adjustmentCacheAfter.Sort(new XSorter());
                var ak = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.afterKeys, adjustmentCacheAfter.Length);
                var av = builder.Allocate(ref glyphBlob.glyphAdjustmentsLookup.afterIndices, adjustmentCacheAfter.Length);
                for (int j = 0; j < ak.Length; j++)
                {
                    var d = adjustmentCacheAfter[j];
                    ak[j] = d.x; //unicode
                    av[j] = d.y;
                }

                hashCounts[BlobTextMeshGlyphExtensions.GetGlyphHash(glyphBlob.unicode)]++;
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

            fontBlobRoot = result.Value;

            return result;
        }
        static bool GlyphIndexToUnicode(uint glyphIndex, List<Character> characterTable, out int unicode)
        {
            unicode = default;
            for (int i = 0, end = characterTable.Count; i < end; i++)
            {
                var currentGlyphIndex = characterTable[i].glyphIndex;
                if (currentGlyphIndex == glyphIndex)
                {
                    unicode = math.asint(characterTable[i].unicode);
                    return true;
                }
            }
            return false;
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

