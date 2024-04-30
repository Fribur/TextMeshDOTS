using Unity.Burst.Intrinsics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using TextMeshDOTS.Rendering.Authoring;

namespace TextMeshDOTS.Rendering
{
    [BurstCompile]
    struct UpdateMultiFontJobChunk : IJobChunk
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
                foreach (var instance in instances)
                {
                    instance.masks.Clear();
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
                    LatiosTextBackendBakingUtility.SetSubMesh(count, ref mmi);
                }
            }
        }
    }
}
