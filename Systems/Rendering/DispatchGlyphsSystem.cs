using TextMeshDOTS.HarfBuzz;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TextMeshDOTS.Rendering
{
    [DisableAutoCreation]
    public partial class DispatchGlyphsSystem : SystemBase
    {
        const int kTextureDimension = 4096;
        const int kShelfAlignment   = 16;

        UnityObjectRef<ComputeShader> m_uploadGlyphsShader;
        UnityObjectRef<ComputeShader> m_copyBytesShader;

        PersistentBuffer         m_glyphsBuffer;
        GraphicsBufferUploadPool m_byteAddressUploadBuffers;

        UnityObjectRef<Texture2DArray> m_sdf8Array;
        UnityObjectRef<Texture2DArray> m_sdf16Array;
        UnityObjectRef<Texture2DArray> m_bitmapArray;

        // Shader bindings
        int _src;
        int _dst;
        int _startOffset;
        int _meta;

        int _tmdSdf8;
        int _tmdSdf16;
        int _tmdBitmap;
        int _tmdGlyphs;

        protected override void OnCreate()
        {
            ref var state = ref CheckedStateRef;

            m_uploadGlyphsShader = Resources.Load<ComputeShader>("UploadGlyphs");
            m_copyBytesShader    = Resources.Load<ComputeShader>("CopyBytes");

            m_glyphsBuffer             = new PersistentBuffer(1024 * 16 * 128, 4, GraphicsBuffer.Target.Raw, m_copyBytesShader);
            m_byteAddressUploadBuffers = new GraphicsBufferUploadPool(1024 * 8 * 4, GraphicsBuffer.Target.Raw, 4);

            m_sdf8Array   = new Texture2DArray(kTextureDimension, kTextureDimension, 2, TextureFormat.R8, false);
            m_sdf16Array  = new Texture2DArray(kTextureDimension, kTextureDimension, 2, TextureFormat.R16, false);
            m_bitmapArray = new Texture2DArray(kTextureDimension, kTextureDimension, 2, TextureFormat.RGBA32, true);

            _src         = Shader.PropertyToID("_src");
            _dst         = Shader.PropertyToID("_dst");
            _startOffset = Shader.PropertyToID("_startOffset");
            _meta        = Shader.PropertyToID("_meta");

            _tmdSdf8   = Shader.PropertyToID("_tmdSdf8");
            _tmdSdf16  = Shader.PropertyToID("_tmdSdf16");
            _tmdBitmap = Shader.PropertyToID("_tmdSdfBitmap");
            _tmdGlyphs = Shader.PropertyToID("_tmdGlyphs");

            var atlas = new AtlasTable(Allocator.Persistent, kTextureDimension, kShelfAlignment);
            EntityManager.CreateSingleton(atlas);
        }

        protected override void OnUpdate()
        {
            ref var state     = ref CheckedStateRef;
            var     collected = Collect(ref state);
            state.CompleteDependency();
            var written = Write(ref state, ref collected);
            state.CompleteDependency();
            Dispatch(ref state, ref written);
        }

        protected override void OnDestroy()
        {
            ref var state = ref CheckedStateRef;

            GraphicsBuffer b = null;
            Shader.SetGlobalBuffer(_tmdGlyphs, b);
            Texture2DArray t = null;
            Shader.SetGlobalTexture(_tmdSdf8,   t);
            Shader.SetGlobalTexture(_tmdSdf16,  t);
            Shader.SetGlobalTexture(_tmdBitmap, t);
        }

        public CollectState Collect(ref SystemState state)
        {
            return default;
        }

        public WriteState Write(ref SystemState state, ref CollectState collected)
        {
            return default;
        }

        public void Dispatch(ref SystemState state, ref WriteState written)
        {
        }

        public struct CollectState
        {
        }

        public struct WriteState
        {
        }
    }
}

