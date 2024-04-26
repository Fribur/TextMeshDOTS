using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace TextMeshDOTS.Rendering
{
    public static unsafe class StaticHelper
    {
        // Helper function to allocate BRG buffers during the BRG callback function
        public static T* Malloc<T>(int count) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                Allocator.TempJob);
        }
        // Raw buffers are allocated in ints, define an utility method to compute the required
        // amount of ints for our data.
        public static int BufferCountForInstances(int bytesPerInstance, int numInstances, int extraBytes = 0)
        {
            // Round byte counts to int multiples
            bytesPerInstance = (bytesPerInstance + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            extraBytes = (extraBytes + sizeof(int) - 1) / sizeof(int) * sizeof(int);
            int totalBytes = bytesPerInstance * numInstances + extraBytes;
            return totalBytes / sizeof(int);
        }
        // Unity provided shaders such as Universal Render Pipeline/Lit expect
        // unity_ObjectToWorld and unity_WorldToObject in a special packed 48 byte
        // format when the DOTS_INSTANCING_ON keyword is enabled.
        // This saves both GPU memory and GPU bandwidth.
        // We define a convenience type here so we can easily convert into this format.
        public static float3x4 GetPackedMatrix(float px, float py, float pz)
        {
            return new float3x4(1, 0, 0, px,
                                0, 1, 0, py,
                                0, 0, 1, pz);
        }
        public static float3x4 GetPackedInverseMatrix(float px, float py, float pz)
        {
            return new float3x4(1, 0, 0, -px,
                                0, 1, 0, -py,
                                0, 0, 1, -pz);
        }
        public static float3x4 GetPackedMatrix(Matrix4x4 m)
        {
            return new float3x4(
                m.m00, m.m01, m.m02, m.m03,
                m.m10, m.m11, m.m12, m.m13,
                m.m20, m.m21, m.m22, m.m23);
        }
        public static float3x4 GetPackedMatrix(float4x4 m)
        {
            var c0 = m.c0;
            var c1 = m.c1;
            var c2 = m.c2;
            var c3 = m.c3;
            return new float3x4(c0.x, c1.x, c2.x, c3.x,
                                c0.y, c1.y, c2.y, c3.y,
                                c0.z, c1.z, c2.z, c3.z);
        }
        public static void BuildIndexBuffer(ref NativeArray<int> indices)
        {
            int glyphCount = indices.Length / 6;
            for (ushort i = 0; i < glyphCount; i++)
            {
                ushort dst = (ushort)(i * 6);
                ushort src = (ushort)(i * 4);
                indices[dst] = src;
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
    }
}
