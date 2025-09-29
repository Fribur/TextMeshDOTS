using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;

namespace TextMeshDOTS
{
    public static class TextMeshDOTSArchetypes
    {
        public static EntityArchetype GetSingleFontTextArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(14, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<TextBaseConfiguration>();
            componentTypeStaging[1] = ComponentType.ReadWrite<GlyphOTF>();
            componentTypeStaging[2] = ComponentType.ReadWrite<CalliByte>();
            componentTypeStaging[3] = ComponentType.ReadWrite<XMLTag>();
            componentTypeStaging[4] = ComponentType.ReadWrite<RenderGlyph>();
            componentTypeStaging[5] = ComponentType.ReadWrite<TextShaderIndex>();
            componentTypeStaging[6] = ComponentType.ReadWrite<LocalTransform>();
            componentTypeStaging[7] = ComponentType.ReadWrite<LocalToWorld>();
            componentTypeStaging[8] = ComponentType.ReadWrite<WorldToLocal_Tag>();
            componentTypeStaging[9] = ComponentType.ReadWrite<WorldRenderBounds>();
            componentTypeStaging[10] = ComponentType.ReadWrite<RenderBounds>();
            componentTypeStaging[11] = ComponentType.ReadWrite<PerInstanceCullingTag>();
            componentTypeStaging[12] = ComponentType.ReadWrite<MaterialMeshInfo>();
            componentTypeStaging[13] = ComponentType.ReadWrite<RenderFilterSettings>();            

            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }
    }
}

