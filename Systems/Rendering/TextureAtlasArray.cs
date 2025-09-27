using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace TextMeshDOTS.Rendering
{
    // Todo: We're doing things the easy way for now. We may want to optimize it in the future to
    // send pixel updates to a compute shader and apply changes on the GPU. However, that is somewhat
    // platform-specific because platforms are inconsistent on whether the first scanline is on top or
    // bottom.
    internal unsafe class TextureAtlasArray<T> : IDisposable where T : unmanaged
    {
        public struct AtlasPtr
        {
            public int atlasIndex;
            public int dimension;
            public T*  ptr;

            public Span<T> AsSpan() => new Span<T>(ptr, dimension * dimension);
        }

        Texture2DArray texture2DArray;
        Texture2DArray oldArray = null;
        int            shaderPropertyId;
        int            dimension;
        int            atlasCount;
        TextureFormat  format;
        bool           useMipmapping;

        public TextureAtlasArray(int shaderPropertyId, int dimension, int initialAtlasCount, TextureFormat format, bool useMipmapping, bool linear)
        {
            this.texture2DArray   = new Texture2DArray(dimension, dimension, initialAtlasCount, format, useMipmapping, linear);
            this.shaderPropertyId = shaderPropertyId;
            this.dimension        = dimension;
            this.atlasCount       = initialAtlasCount;
            this.format           = format;
            this.useMipmapping    = useMipmapping;
            for (int i = 0; i < initialAtlasCount; i++)
            {
                texture2DArray.GetPixelData<T>(0, i).AsSpan().Clear();
            }
            texture2DArray.Apply(useMipmapping, false);
        }

        public void Dispose()
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(texture2DArray);
            else
                UnityEngine.Object.DestroyImmediate(texture2DArray);
        }

        public void GetAtlasPtrsForDirtyIndices(ReadOnlySpan<uint> dirtyIndicesSorted, Span<AtlasPtr> ptrs)
        {
            var atlasesNeeded = 1 + (int)(dirtyIndicesSorted[dirtyIndicesSorted.Length - 1] & 0x3fffffffu);
            if (atlasesNeeded >= atlasCount)
            {
                oldArray       = texture2DArray;
                texture2DArray = new Texture2DArray(dimension, dimension, atlasesNeeded, format, useMipmapping);

                for (int i = 0; i < atlasCount; i++)
                {
                    texture2DArray.CopyPixels(oldArray, i, 0, i, 0);
                }

                atlasCount = atlasesNeeded;
            }
            for (int i = 0; i < dirtyIndicesSorted.Length; i++)
            {
                var atlasIndex = (int)(dirtyIndicesSorted[i] & 0x3fffffffu);
                ptrs[i]        = new AtlasPtr
                {
                    atlasIndex = atlasIndex,
                    dimension  = dimension,
                    ptr        = (T*)texture2DArray.GetPixelData<T>(0, atlasIndex).GetUnsafePtr()
                };
            }
        }

        public void ApplyChanges()
        {
            texture2DArray.Apply(useMipmapping, false);
            Shader.SetGlobalTexture(shaderPropertyId, texture2DArray);
            if (oldArray != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(oldArray);
                else
                    UnityEngine.Object.DestroyImmediate(oldArray);
            }
        }
    }
}

