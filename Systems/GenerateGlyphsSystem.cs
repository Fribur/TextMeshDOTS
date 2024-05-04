using UnityEngine;
using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace TextMeshDOTS
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    public partial struct GenerateGlyphsSystem : ISystem
    {
        EntityQuery m_query;

        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_query = SystemAPI.QueryBuilder()
                      .WithAll<FontBlobReference>()
                      .WithAllRW<RenderGlyph>()
                      .WithAll<CalliByte>()
                      .WithAll<TextBaseConfiguration>()
                      .WithAllRW<TextRenderControl>()
                      .Build();
            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new Job
            {
                additionalEntitiesHandle    = SystemAPI.GetBufferTypeHandle<AdditionalFontMaterialEntity>(true),
                calliByteHandle             = SystemAPI.GetBufferTypeHandle<CalliByte>(true),
                fontBlobReferenceHandle     = SystemAPI.GetComponentTypeHandle<FontBlobReference>(true),
                fontBlobReferenceLookup     = SystemAPI.GetComponentLookup<FontBlobReference>(true),
                glyphMappingElementHandle   = SystemAPI.GetBufferTypeHandle<GlyphMappingElement>(false),
                glyphMappingMaskHandle      = SystemAPI.GetComponentTypeHandle<GlyphMappingMask>(true),
                lastSystemVersion           = m_skipChangeFilter ? 0 : state.LastSystemVersion,
                renderGlyphHandle           = SystemAPI.GetBufferTypeHandle<RenderGlyph>(false),
                selectorHandle              = SystemAPI.GetBufferTypeHandle<FontMaterialSelectorForGlyph>(false),
                textBaseConfigurationHandle = SystemAPI.GetComponentTypeHandle<TextBaseConfiguration>(true),
                textRenderControlHandle     = SystemAPI.GetComponentTypeHandle<TextRenderControl>(false),
            }.ScheduleParallel(m_query, state.Dependency);
        }

        [BurstCompile]
        public partial struct Job : IJobChunk
        {
            public BufferTypeHandle<RenderGlyph>                  renderGlyphHandle;
            public BufferTypeHandle<GlyphMappingElement>          glyphMappingElementHandle;
            public BufferTypeHandle<FontMaterialSelectorForGlyph> selectorHandle;
            public ComponentTypeHandle<TextRenderControl>         textRenderControlHandle;

            [ReadOnly] public ComponentTypeHandle<GlyphMappingMask>          glyphMappingMaskHandle;
            [ReadOnly] public BufferTypeHandle<CalliByte>                    calliByteHandle;
            [ReadOnly] public ComponentTypeHandle<TextBaseConfiguration>     textBaseConfigurationHandle;
            [ReadOnly] public ComponentTypeHandle<FontBlobReference>         fontBlobReferenceHandle;
            [ReadOnly] public BufferTypeHandle<AdditionalFontMaterialEntity> additionalEntitiesHandle;
            [ReadOnly] public ComponentLookup<FontBlobReference>             fontBlobReferenceLookup;

            public uint lastSystemVersion;

            private GlyphMappingWriter m_glyphMappingWriter;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!(chunk.DidChange(ref glyphMappingMaskHandle, lastSystemVersion) ||
                      chunk.DidChange(ref calliByteHandle, lastSystemVersion) ||
                      chunk.DidChange(ref textBaseConfigurationHandle, lastSystemVersion) ||
                      chunk.DidChange(ref fontBlobReferenceHandle, lastSystemVersion)))
                    return;

                var calliBytesBuffers      = chunk.GetBufferAccessor(ref calliByteHandle);
                var renderGlyphBuffers     = chunk.GetBufferAccessor(ref renderGlyphHandle);
                var glyphMappingBuffers    = chunk.GetBufferAccessor(ref glyphMappingElementHandle);
                var glyphMappingMasks      = chunk.GetNativeArray(ref glyphMappingMaskHandle);
                var textBaseConfigurations = chunk.GetNativeArray(ref textBaseConfigurationHandle);
                var fontBlobReferences     = chunk.GetNativeArray(ref fontBlobReferenceHandle);
                var textRenderControls     = chunk.GetNativeArray(ref textRenderControlHandle);

                // Optional
                var  selectorBuffers           = chunk.GetBufferAccessor(ref selectorHandle);
                var  additionalEntitiesBuffers = chunk.GetBufferAccessor(ref additionalEntitiesHandle);
                bool hasMultipleFonts          = selectorBuffers.Length > 0 && additionalEntitiesBuffers.Length > 0;

                FontMaterialSet fontMaterialSet = default;

                for (int indexInChunk = 0; indexInChunk < chunk.Count; indexInChunk++)
                {
                    var calliBytes            = calliBytesBuffers[indexInChunk];
                    var renderGlyphs          = renderGlyphBuffers[indexInChunk];
                    var fontBlobReference     = fontBlobReferences[indexInChunk];
                    var textBaseConfiguration = textBaseConfigurations[indexInChunk];
                    var textRenderControl     = textRenderControls[indexInChunk];

                    m_glyphMappingWriter.StartWriter(glyphMappingMasks.Length > 0 ? glyphMappingMasks[indexInChunk].mask : default);
                    if (hasMultipleFonts)
                    {
                        fontMaterialSet.Initialize(fontBlobReference.fontBlob, selectorBuffers[indexInChunk], additionalEntitiesBuffers[indexInChunk], ref fontBlobReferenceLookup);
                    }
                    else
                    {
                        fontMaterialSet.Initialize(fontBlobReference.fontBlob);
                    }

                    GlyphGeneration.CreateRenderGlyphs(ref renderGlyphs,
                                                       ref m_glyphMappingWriter,
                                                       ref fontMaterialSet,
                                                       in calliBytes,
                                                       in textBaseConfiguration);

                    if (glyphMappingBuffers.Length > 0)
                    {
                        var mapping = glyphMappingBuffers[indexInChunk];
                        m_glyphMappingWriter.EndWriter(ref mapping, renderGlyphs.Length);
                    }

                    textRenderControl.flags          = TextRenderControl.Flags.Dirty;
                    textRenderControls[indexInChunk] = textRenderControl;
                }
            }
        }
    }
}

