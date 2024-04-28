using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using static Unity.Entities.SystemAPI;

namespace TextMeshDOTS.Rendering.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct TextRenderingUpdateSystem : ISystem
    {
        EntityQuery m_singleFontQuery;
        EntityQuery m_multiFontQuery;
        bool m_skipChangeFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_singleFontQuery = QueryBuilder()
                     .WithAll<RenderGlyph>()
                     .WithAllRW<RenderBounds>()
                     .WithAllRW<TextRenderControl>()
                     .WithAllRW<MaterialMeshInfo>()
                     .WithAbsent<FontMaterialSelectorForGlyph>()
                     .Build();
            m_multiFontQuery = QueryBuilder()
                     .WithAll<RenderGlyph>()
                     .WithAll<FontMaterialSelectorForGlyph>()
                     .WithAllRW<RenderBounds>()
                     .WithAllRW<TextRenderControl>()
                     .WithAllRW<MaterialMeshInfo>()
                     .Build();

            m_skipChangeFilter = (state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!TryGetSingletonEntity<TextStatisticsTag>(out Entity textStats))
                return;            

            state.EntityManager.SetComponentData(textStats, new GlyphCountThisFrame { glyphCount = 0 });
            state.EntityManager.SetComponentData(textStats, new MaskCountThisFrame { maskCount = 1 });  // Zero reserved for no mask

            state.Dependency = new SingleFontJob
            {
                glyphHandle = GetBufferTypeHandle<RenderGlyph>(true),
                boundsHandle = GetComponentTypeHandle<RenderBounds>(false),
                controlHandle = GetComponentTypeHandle<TextRenderControl>(false),
                materialMeshInfoHandle = GetComponentTypeHandle<MaterialMeshInfo>(false),
                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion
            }.ScheduleParallel(m_singleFontQuery, state.Dependency);

            state.Dependency = new MultiFontJob
            {
                additionalEntityHandle = GetBufferTypeHandle<AdditionalFontMaterialEntity>(true),
                boundsLookup = GetComponentLookup<RenderBounds>(false),
                controlLookup = GetComponentLookup<TextRenderControl>(false),
                entityHandle = GetEntityTypeHandle(),
                glyphHandle = GetBufferTypeHandle<RenderGlyph>(true),
                glyphMaskLookup = GetBufferLookup<RenderGlyphMask>(false),
                lastSystemVersion = m_skipChangeFilter ? 0 : state.LastSystemVersion,
                materialMeshInfoLookup = GetComponentLookup<MaterialMeshInfo>(false),
                selectorHandle = GetBufferTypeHandle<FontMaterialSelectorForGlyph>(true)
            }.ScheduleParallel(m_multiFontQuery, state.Dependency);
        }


        [BurstCompile]
        struct SingleFontJob : IJobChunk
        {
            [ReadOnly] public BufferTypeHandle<RenderGlyph> glyphHandle;
            public ComponentTypeHandle<RenderBounds> boundsHandle;
            public ComponentTypeHandle<TextRenderControl> controlHandle;
            public ComponentTypeHandle<MaterialMeshInfo> materialMeshInfoHandle;
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
                var mmis = chunk.GetComponentDataPtrRW(ref materialMeshInfoHandle);
                var glyphBuffers = chunk.GetBufferAccessor(ref glyphHandle);

                for (int entity = firstEntityNeedingUpdate; entity < chunk.Count; entity++)
                {
                    if ((ctrlRW[entity].flags & TextRenderControl.Flags.Dirty) != TextRenderControl.Flags.Dirty)
                        continue;
                    ctrlRW[entity].flags &= ~TextRenderControl.Flags.Dirty;

                    var glyphBuffer = glyphBuffers[entity].AsNativeArray();
                    var aabb = new Aabb { Min = float.MaxValue, Max = float.MinValue };
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

                    ref var mmi = ref mmis[entity];
                    var value = glyphBuffer.Length;
                    StaticHelper.SetSubMesh(glyphBuffer.Length, ref mmi);
                }
            }
        }

        [BurstCompile]
        struct MultiFontJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityHandle;
            [ReadOnly] public BufferTypeHandle<RenderGlyph> glyphHandle;
            [ReadOnly] public BufferTypeHandle<FontMaterialSelectorForGlyph> selectorHandle;
            [ReadOnly] public BufferTypeHandle<AdditionalFontMaterialEntity> additionalEntityHandle;
            [NativeDisableParallelForRestriction] public ComponentLookup<RenderBounds> boundsLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<TextRenderControl> controlLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<MaterialMeshInfo> materialMeshInfoLookup;
            [NativeDisableParallelForRestriction] public BufferLookup<RenderGlyphMask> glyphMaskLookup;
            public uint lastSystemVersion;

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityHandle);
                if (!controlLookup.DidChange(entities[0], lastSystemVersion))
                    return;

                var glyphBuffers = chunk.GetBufferAccessor(ref glyphHandle);
                var selectorBuffers = chunk.GetBufferAccessor(ref selectorHandle);
                var entityBuffers = chunk.GetBufferAccessor(ref additionalEntityHandle);

                FixedList4096Bytes<FontMaterialInstance> instances = default;

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entities[entityIndex];

                    var ctrl = controlLookup[entity];
                    if ((ctrl.flags & TextRenderControl.Flags.Dirty) != TextRenderControl.Flags.Dirty)
                        continue;
                    ctrl.flags &= ~TextRenderControl.Flags.Dirty;

                    var glyphBuffer = glyphBuffers[entityIndex].AsNativeArray();
                    var selectorBuffer = selectorBuffers[entityIndex].AsNativeArray().Reinterpret<byte>();
                    var entityBuffer = entityBuffers[entityIndex].AsNativeArray().Reinterpret<Entity>();

                    instances.Add(new FontMaterialInstance
                    {
                        masks = glyphMaskLookup[entity].Reinterpret<uint>(),
                        aabb = new Aabb { Min = float.MaxValue, Max = float.MinValue },
                        entity = entity
                    });

                    for (int i = 0; i < entityBuffer.Length; i++)
                    {
                        instances.Add(new FontMaterialInstance
                        {
                            masks = glyphMaskLookup[entityBuffer[i]].Reinterpret<uint>(),
                            aabb = new Aabb { Min = float.MaxValue, Max = float.MinValue },
                            entity = entityBuffer[i]
                        });
                    }

                    var glyphCount = math.min(glyphBuffer.Length, selectorBuffer.Length);
                    for (int i = 0; i < glyphCount; i++)
                    {
                        var glyph = glyphBuffer[i];
                        var c = (glyph.blPosition + glyph.trPosition) / 2f;
                        var e = math.length(c - glyph.blPosition);
                        e += glyph.shear;

                        var selectIndex = selectorBuffer[i];
                        ref var instance = ref instances.ElementAt(selectIndex);

                        instance.aabb.Include(new Aabb { Min = new float3(c - e, 0f), Max = new float3(c + e, 0f) });

                        if (instance.masks.Length > 0)
                        {
                            ref var lastMask = ref instance.masks.ElementAt(instance.masks.Length - 1);
                            var offset = lastMask & 0xffff;
                            if (i - offset < 16)
                            {
                                var bit = i - offset + 16;
                                lastMask |= 1u << (byte)bit;
                                continue;
                            }
                        }
                        instance.masks.Add((uint)i + 0x10000);
                    }

                    for (int i = 0; i < instances.Length; i++)
                    {
                        ref var instance = ref instances.ElementAt(i);

                        if (glyphBuffer.Length == 0)
                        {
                            instance.aabb.Min = 0f;
                            instance.aabb.Max = 0f;
                        }
                        boundsLookup[instance.entity] = new RenderBounds { Value = new AABB { Center = instance.aabb.Center, Extents = instance.aabb.Extents } };
                        controlLookup[instance.entity] = ctrl;
                        ref var mmi = ref materialMeshInfoLookup.GetRefRW(instance.entity).ValueRW;

                        var quadCount = instance.masks.Length * 16;
                        if (quadCount == 16)
                            quadCount = math.countbits(instance.masks[0] & 0xffff0000);

                        var count = quadCount <= 8 ? quadCount : glyphBuffer.Length;
                        StaticHelper.SetSubMesh(count, ref mmi);
                    }
                }
            }

            struct FontMaterialInstance
            {
                public DynamicBuffer<uint> masks;
                public Aabb aabb;
                public Entity entity;
            }
        }
    }
}

