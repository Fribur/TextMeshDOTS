using TextMeshDOTS.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;

namespace TextMeshDOTS
{
    static class TextMeshDOTSArchetypes
    {
        //These singleton components will be added to TextRenderingUpdateSystem in OnCreate()
        public static ComponentTypeSet GetTextStatisticsTypeset()
        {
            var result = new FixedList128Bytes<ComponentType>
            {
                ComponentType.ReadWrite<GlyphCountThisFrame>(),
                ComponentType.ReadWrite<MaskCountThisFrame>(),
                ComponentType.ReadWrite<TextStatisticsTag>(),
            };
            return new ComponentTypeSet(result);
        }

        public static EntityArchetype GetTextRenderArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(14, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<LocalTransform>();
            componentTypeStaging[1] = ComponentType.ReadWrite<LocalToWorld>();
            componentTypeStaging[2] = ComponentType.ReadWrite<FontBlobReference>();
            componentTypeStaging[3] = ComponentType.ReadWrite<TextBaseConfiguration>();
            componentTypeStaging[4] = ComponentType.ReadWrite<TextRenderControl>();
            componentTypeStaging[5] = ComponentType.ReadWrite<CalliByte>();
            componentTypeStaging[6] = ComponentType.ReadWrite<RenderGlyph>();
            componentTypeStaging[7] = ComponentType.ReadWrite<TextShaderIndex>();
            componentTypeStaging[8] = ComponentType.ReadWrite<WorldToLocal_Tag>();
            componentTypeStaging[9] = ComponentType.ReadWrite<WorldRenderBounds>();
            componentTypeStaging[10] = ComponentType.ReadWrite<RenderBounds>();
            componentTypeStaging[11] = ComponentType.ReadWrite<PerInstanceCullingTag>();
            componentTypeStaging[12] = ComponentType.ReadWrite<MaterialMeshInfo>();
            componentTypeStaging[13] = ComponentType.ReadWrite<RenderFilterSettings>();

            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }
        public static EntityArchetype GetTextBRGArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(9, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<LocalTransform>();
            componentTypeStaging[1] = ComponentType.ReadWrite<LocalToWorld>();
            componentTypeStaging[2] = ComponentType.ReadWrite<FontBlobReference>();
            componentTypeStaging[3] = ComponentType.ReadWrite<TextBaseConfiguration>();
            componentTypeStaging[4] = ComponentType.ReadWrite<TextRenderControl>();
            componentTypeStaging[5] = ComponentType.ReadWrite<CalliByte>();
            componentTypeStaging[6] = ComponentType.ReadWrite<RenderGlyph>();
            componentTypeStaging[7] = ComponentType.ReadWrite<TextShaderIndex>();
            componentTypeStaging[8] = ComponentType.ReadWrite<RenderBounds>();

            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }
    }
}

