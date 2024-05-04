using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace TextMeshDOTS.Rendering.Authoring
{
    [BurstCompile]
    public static class TextBackendBakingUtility
    {        
        public const string kResourcePath = "Assets/Resources";
        //public const string kTextBackendMeshPath     = "Packages/com.textmeshdots/Resources/TextBackendMesh.mesh";
        public const string kTextBackendMeshPath = "Assets/Resources/TextBackendMesh.mesh";
        public const string kTextBackendMeshResource = "TextBackendMesh";

        #region Mesh Building
#if UNITY_EDITOR
        [UnityEditor.MenuItem("TextMeshDOTS/Text BackendMesh")]
        static void CreateMeshAsset()
        {
            var glyphCounts = new NativeArray<int>(16, Allocator.Temp);
            glyphCounts[0] = 4;
            glyphCounts[1] = 8;
            glyphCounts[2] = 16;
            glyphCounts[3] = 24;
            glyphCounts[4] = 32;
            glyphCounts[5] = 48;
            glyphCounts[6] = 64;
            glyphCounts[7] = 96;
            glyphCounts[8] = 128;
            glyphCounts[9] = 256;
            glyphCounts[10] = 512;
            glyphCounts[11] = 1024;
            glyphCounts[12] = 2048;
            glyphCounts[13] = 4096;
            glyphCounts[14] = 8192;
            glyphCounts[15] = 16384;

            var mesh = CreateMesh(16384, glyphCounts);
            if(!UnityEditor.AssetDatabase.IsValidFolder(kResourcePath))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            UnityEditor.AssetDatabase.CreateAsset(mesh, kTextBackendMeshPath);
        }
#endif

        internal static unsafe Mesh CreateMesh(int glyphCount, NativeArray<int> glyphCountsBySubmesh)
        {
            Mesh mesh      = new Mesh();
            var  f3Pattern = new NativeArray<float3>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            f3Pattern[0]   = new float3(-1f, 0f, 0f);
            f3Pattern[1]   = new float3(0f, 1f, 0f);
            f3Pattern[2]   = new float3(1f, 1f, 0f);
            f3Pattern[3]   = new float3(1f, 0f, 0f);
            var f3s        = new NativeArray<float3>(glyphCount * 4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpyReplicate(f3s.GetUnsafePtr(), f3Pattern.GetUnsafePtr(), 48, glyphCount);
            mesh.SetVertices(f3s);
            var f4Pattern = new NativeArray<float4>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            f4Pattern[0]  = new float4(0f, 0f, 0f, 0f);
            f4Pattern[1]  = new float4(0f, 1f, 0f, 0f);
            f4Pattern[2]  = new float4(1f, 1f, 0f, 0f);
            f4Pattern[3]  = new float4(1f, 0f, 0f, 0f);
            var f4s       = new NativeArray<float4>(glyphCount * 4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpyReplicate(f4s.GetUnsafePtr(), f4Pattern.GetUnsafePtr(), 64, glyphCount);
            mesh.SetUVs(0, f4s);
            mesh.SetColors(f4s);
            var f2Pattern = new NativeArray<float2>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            f2Pattern[0]  = new float2(0f, 0f);
            f2Pattern[1]  = new float2(0f, 1f);
            f2Pattern[2]  = new float2(1f, 1f);
            f2Pattern[3]  = new float2(1f, 0f);
            var f2s       = new NativeArray<float2>(glyphCount * 4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpyReplicate(f2s.GetUnsafePtr(), f2Pattern.GetUnsafePtr(), 32, glyphCount);
            mesh.SetUVs(2, f2s);

            mesh.subMeshCount = glyphCountsBySubmesh.Length;
            for (int submesh = 0; submesh < glyphCountsBySubmesh.Length; submesh++)
            {
                var indices = new NativeArray<ushort>(glyphCountsBySubmesh[submesh] * 6, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                BuildIndexBuffer(ref indices);
                mesh.SetIndices(indices, MeshTopology.Triangles, submesh);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.UploadMeshData(true);

            return mesh;
        }

        [BurstCompile]
        public static void BuildIndexBuffer(ref NativeArray<ushort> indices)
        {
            int glyphCount = indices.Length / 6;
            for (ushort i = 0; i < glyphCount; i++)
            {
                ushort dst       = (ushort)(i * 6);
                ushort src       = (ushort)(i * 4);
                indices[dst]     = src;
                indices[dst + 1] = (ushort)(src + 1);
                indices[dst + 2] = (ushort)(src + 2);
                indices[dst + 3] = (ushort)(src + 2);
                indices[dst + 4] = (ushort)(src + 3);
                indices[dst + 5] = src;
            }
        }
        public static void SetSubMesh(int glyphCount, ref MaterialMeshInfo mmi)
        {
            switch (glyphCount)
            {
                case int _ when glyphCount <= 4:
                    mmi.SubMesh = 0; break;
                case int _ when glyphCount <= 8:
                    mmi.SubMesh = 1; break;
                case int _ when glyphCount <= 16:
                    mmi.SubMesh = 2; break;
                case int _ when glyphCount <= 24:
                    mmi.SubMesh = 3; break;
                case int _ when glyphCount <= 32:
                    mmi.SubMesh = 4; break;
                case int _ when glyphCount <= 48:
                    mmi.SubMesh = 5; break;
                case int _ when glyphCount <= 64:
                    mmi.SubMesh = 6; break;
                case int _ when glyphCount <= 96:
                    mmi.SubMesh = 7; break;
                case int _ when glyphCount <= 128:
                    mmi.SubMesh = 8; break;
                case int _ when glyphCount <= 256:
                    mmi.SubMesh = 9; break;
                case int _ when glyphCount <= 512:
                    mmi.SubMesh = 10; break;
                case int _ when glyphCount <= 1024:
                    mmi.SubMesh = 11; break;
                case int _ when glyphCount <= 2048:
                    mmi.SubMesh = 12; break;
                case int _ when glyphCount <= 4096:
                    mmi.SubMesh = 13; break;
                case int _ when glyphCount <= 8192:
                    mmi.SubMesh = 14; break;
                case int _ when glyphCount <= 16384:
                    mmi.SubMesh = 15; break;
                default:
                    mmi.SubMesh = 15;
                    UnityEngine.Debug.LogWarning("Glyphs in RenderGlyph buffer exceeds max capacity of 16384 and will be truncated.");
                    break;
            }
        }
        #endregion
    }
}

