using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;

namespace TextMeshDOTS.Rendering
{
    //[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public unsafe partial class TextRenderingDispatchSystem : SystemBase
    {
        EntityQuery textToRenderQ;

        //Shader properties valid for entire batch uploaded via SetGlobalBuffer
        GraphicsBuffer gpuLatiosTextBuffer;
        NativeList<RenderGlyph> cpuLatiosTextBuffer;
        //corresponding shader property IDs
        int latiosTextBufferID;

        bool _initialized;

        protected override void OnCreate()
        {
            textToRenderQ = SystemAPI.QueryBuilder()
                        .WithAllRW<TextShaderIndex>()
                        .WithAll<TextRenderControl>()
                        .WithAll<LocalToWorld>()
                        .WithAll<RenderBounds>()
                        .WithAll<RenderGlyph>()
                        .Build();
            RequireForUpdate(textToRenderQ);
            GetShaderPropertyIDs();
        }
        protected override void OnUpdate()
        {
            //To-Do: copy implementation from Calligraphics to update latiosTextBuffer when chunks have changed
            if (!_initialized)
            {
                UploadGlyphDataToGPU();
                Debug.Log("Initialzied text Entities Graphics");
            }
        }
        protected override void OnDestroy()
        {
            if (_initialized)
            {
                gpuLatiosTextBuffer.Dispose();
                cpuLatiosTextBuffer.Dispose();
            }
        }

        void UploadGlyphDataToGPU()
        {
            var textRenderCount = textToRenderQ.CalculateEntityCount();
            cpuLatiosTextBuffer = new NativeList<RenderGlyph>(textRenderCount * 64, Allocator.Persistent);

            //fetch data for setting the instanced batch
            var CollectGlyphDataJob = new CollectEGGlyphDataJob
            {
                firstGlyphIndex = 0,
                renderGlyphs = cpuLatiosTextBuffer,
            };
            Dependency = CollectGlyphDataJob.Schedule(textToRenderQ, Dependency);
            Dependency.Complete();

            //setup shader properties that are uploaded into global buffer providing data valid for all instances
            var UseConstantBuffer = false;
            var target = UseConstantBuffer ? GraphicsBuffer.Target.Constant : GraphicsBuffer.Target.Raw;
            gpuLatiosTextBuffer = new GraphicsBuffer(target, cpuLatiosTextBuffer.Length, 96);
            gpuLatiosTextBuffer.SetData(cpuLatiosTextBuffer.AsArray());
            if (UseConstantBuffer)
                Shader.SetGlobalConstantBuffer(latiosTextBufferID, gpuLatiosTextBuffer, 0, cpuLatiosTextBuffer.Length * 4 * 4);
            else
                Shader.SetGlobalBuffer(latiosTextBufferID, gpuLatiosTextBuffer);

            _initialized = true;
        }


        void GetShaderPropertyIDs()
        {
            latiosTextBufferID = Shader.PropertyToID("_latiosTextBuffer");  //global property
        }
    }
}
