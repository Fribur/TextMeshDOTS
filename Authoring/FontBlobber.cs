using System.Collections.Generic;
using UnityEngine;
using TextMeshDOTS.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
            fontBlobRoot.tabWidth            = font.faceInfo.tabWidth;
            fontBlobRoot.tabMultiple         = font.tabMultiple;
            fontBlobRoot.regularStyleSpacing = font.regularStyleSpacing;
            fontBlobRoot.regularStyleWeight  = font.regularStyleWeight;
            fontBlobRoot.boldStyleSpacing    = font.boldStyleSpacing;
            fontBlobRoot.boldStyleWeight     = font.boldStyleWeight;
            fontBlobRoot.italicsStyleSlant   = font.italicStyleSlant;            
            fontBlobRoot.atlasWidth          = font.atlasWidth;
            fontBlobRoot.atlasHeight         = font.atlasHeight;
            fontBlobRoot.materialPadding     = materialPadding;
      
            var characterLookupTable = font.characterLookupTable;
            var characterHashMapBuilder = builder.AllocateHashMap(ref fontBlobRoot.characters, characterLookupTable.Count);
            foreach (var character in characterLookupTable)
            {
                var glyph = character.Value.glyph;
                characterHashMapBuilder.Add((int)character.Key, new GlyphBlob { glyphMetrics = glyph.metrics, glyphRect = glyph.glyphRect, glyphScale = glyph.scale });
            }

            var glyphPairAdjustments = font.GetGlyphPairAdjustmentRecords();
            var adjustementPairHashMap = new NativeParallelHashMap<long, AdjustmentPair>(characterLookupTable.Count, Allocator.Temp);            
            foreach (var kerningPair in glyphPairAdjustments)
            {
                if (GlyphIndexToUnicode(kerningPair.firstAdjustmentRecord.glyphIndex, characterLookupTable, out int firstUnicode) &&
                    GlyphIndexToUnicode(kerningPair.secondAdjustmentRecord.glyphIndex, characterLookupTable, out int secondUnicode))
                {
                    long key = ((long)secondUnicode << 32) | (long)firstUnicode;
                    var adj1 = kerningPair.firstAdjustmentRecord.glyphValueRecord;
                    var firstAdjustment = new GlyphAdjustment { xPlacement = adj1.xPlacement, yPlacement = adj1.yPlacement, xAdvance = adj1.xAdvance, yAdvance = adj1.yAdvance, };
                    var adj2 = kerningPair.secondAdjustmentRecord.glyphValueRecord;
                    var secondAdjustment = new GlyphAdjustment { xPlacement = adj2.xPlacement, yPlacement = adj2.yPlacement, xAdvance = adj2.xAdvance, yAdvance = adj2.yAdvance, };
                    var adjustmentPair = new AdjustmentPair { firstAdjustment = firstAdjustment, secondAdjustment = secondAdjustment, fontFeatureLookupFlags = kerningPair.featureLookupFlags};
                    if(!adjustementPairHashMap.ContainsKey(key))
                        adjustementPairHashMap.Add(key, adjustmentPair);
                }
            }
            if(adjustementPairHashMap.Count() > 0)
                BlobBuilderExtensions.ConstructHashMap(ref builder, ref fontBlobRoot.adjustmentPairs, ref adjustementPairHashMap);
            else
                builder.AllocateHashMap(ref fontBlobRoot.adjustmentPairs, 1, 1);//allocate empty dummy hashmap to ensure kerning lookup function does not break

            var result = builder.CreateBlobAssetReference<FontBlob>(Allocator.Persistent);
            builder.Dispose();
            fontBlobRoot = result.Value;
            return result;
        }
        static bool GlyphIndexToUnicode(uint glyphIndex, Dictionary<uint, Character> characterLookupTable, out int unicode)
        {
            unicode = default;
            foreach (var character in characterLookupTable.Values)
            {
                if (character.glyphIndex == glyphIndex)
                {
                    unicode = math.asint(character.unicode);
                    return true;
                }
            }
            return false;
        }        
    }
}