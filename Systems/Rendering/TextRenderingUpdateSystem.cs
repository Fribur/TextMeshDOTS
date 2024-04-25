using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

using static Unity.Entities.SystemAPI;

namespace TextMeshDOTS.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct TextRenderingUpdateSystem : ISystem
    {
        EntityQuery m_singleFontQuery;
        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_singleFontQuery = QueryBuilder()
                      .WithAll<RenderGlyph>()
                      .WithAllRW<RenderBounds>()
                      .WithAllRW<TextRenderControl>()
                      .WithAbsent<FontMaterialSelectorForGlyph>()
                      .Build();

            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SingleFontJob
            {
                glyphHandle = GetBufferTypeHandle<RenderGlyph>(true),
                boundsHandle = GetComponentTypeHandle<RenderBounds>(false),
                controlHandle = GetComponentTypeHandle<TextRenderControl>(false),
                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion
            }.ScheduleParallel(m_singleFontQuery, state.Dependency);
        }

        [BurstCompile]
        struct SingleFontJob : IJobChunk
        {
            [ReadOnly] public BufferTypeHandle<RenderGlyph> glyphHandle;
            public ComponentTypeHandle<RenderBounds> boundsHandle;
            public ComponentTypeHandle<TextRenderControl> controlHandle;
            public uint lastSystemVersion;

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!chunk.DidChange(ref controlHandle, lastSystemVersion))
                    return;

                var ctrlRO = chunk.GetComponentDataPtrRO(ref controlHandle);
                int firstEntityNeedingUpdate = 0;
                for (; firstEntityNeedingUpdate < chunk.Count; firstEntityNeedingUpdate++)
                {
                    if ((ctrlRO[firstEntityNeedingUpdate].flags & TextRenderControl.Flags.Dirty) == TextRenderControl.Flags.Dirty)
                        break;
                }
                if (firstEntityNeedingUpdate >= chunk.Count)
                    return;

                var ctrlRW = chunk.GetComponentDataPtrRW(ref controlHandle);
                var bounds = chunk.GetComponentDataPtrRW(ref boundsHandle);
                var glyphBuffers = chunk.GetBufferAccessor(ref glyphHandle);

                for (int entity = firstEntityNeedingUpdate; entity < chunk.Count; entity++)
                {
                    if ((ctrlRW[entity].flags & TextRenderControl.Flags.Dirty) != TextRenderControl.Flags.Dirty)
                        continue;
                    ctrlRW[entity].flags &= ~TextRenderControl.Flags.Dirty;

                    var glyphBuffer = glyphBuffers[entity].AsNativeArray();
                    Aabb aabb = new Aabb { Min = float.MaxValue, Max = float.MinValue };
                    for (int i = 0; i < glyphBuffer.Length; i++)
                    {
                        var glyph = glyphBuffer[i];
                        var c = (glyph.blPosition + glyph.trPosition) / 2f;
                        var e = math.length(c - glyph.blPosition);
                        e += glyph.shear;
                        aabb.Include(new Aabb { Min = new float3(c - e, 0f), Max = new float3(c + e, 0f) });
                    }

                    if (glyphBuffer.Length == 0)
                    {
                        aabb.Min = 0f;
                        aabb.Max = 0f;
                    }
                    bounds[entity] = new RenderBounds { Value = new AABB { Center = aabb.Center, Extents = aabb.Extents } };
                }
            }
        }
    }
}


