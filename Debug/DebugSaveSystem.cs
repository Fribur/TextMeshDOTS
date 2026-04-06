using System.IO;
using TextMeshDOTS.HarfBuzz;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace TextMeshDOTS
{
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct SaveSystem : ISystem
    {
        int saveCount;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            saveCount = 0;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<InputStates>(out InputStates inputStates))
                return;
            
            if (!(inputStates.save == ButtonState.Canceled))
                return;

            var glyphTable = SystemAPI.GetSingleton<GlyphTable>();
            var glyphGpuTable = SystemAPI.GetSingleton<GlyphGpuTable>();
            var atlasTable = SystemAPI.GetSingleton<AtlasTable>();
            var fontTable = SystemAPI.GetSingleton<FontTable>();

            if (glyphTable.glyphEntries.Length == 0) 
                return;

            saveCount++;

            StreamWriter writer = new StreamWriter($"Atlas Debug {saveCount}.txt", false);
            for (int i = 0, ii = glyphTable.glyphEntries.Length; i < ii; i++)
            {
                var entry = glyphTable.glyphEntries[i];                
                var key = entry.key;
                if(key.format != RenderFormat.SDF8)
                    continue;
                var paddedAtlasRect = entry.PaddedAtlasRect;
                var face = fontTable.faces[key.faceIndex];
                var fontFamily = face.GetName(NameID.FONT_FAMILY, Language.English);
                var fontSubFamily = face.GetName(NameID.FONT_SUBFAMILY, Language.English);
                var debugString = $"glyphID: {key.glyphIndex} ({fontFamily} {fontSubFamily}) {key.format} refCount: {entry.refCount} atlasPos: {paddedAtlasRect.x} {paddedAtlasRect.y} {paddedAtlasRect.width} {paddedAtlasRect.height}";
                writer.WriteLine(debugString);
                Debug.Log(debugString);
            }
            //for (int i = 0, ii = atlasTable.sdf8Shelves.Length; i < ii; i++)
            //{
            //    var shelf = atlasTable.sdf8Shelves[i];
            //    var debugString = $"usedX: {shelf.usedX} (reservedX: {shelf.reservedX}) shelf: y: {shelf.y} z: {shelf.z} height: {shelf.height}";
            //    writer.WriteLine(debugString);
            //    Debug.Log(debugString);
            //    for (int k = 0, kk = shelf.gaps.Length; k < kk; k++)
            //    {
            //        var gap = shelf.gaps[k];
            //        Debug.Log($"{gap.x} {gap.y}");
            //    }
            //}
            writer.WriteLine();
            writer.Close();
        }
    }
}
