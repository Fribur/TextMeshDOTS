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
        //These singleton components will be added to TextRenderingUpdateSystem in OnCreate()
        internal static ComponentTypeSet GetTextStatisticsTypeset()
        {
            var result = new FixedList128Bytes<ComponentType>
            {
                ComponentType.ReadWrite<GlyphCountThisFrame>(),
                ComponentType.ReadWrite<MaskCountThisFrame>(),
                ComponentType.ReadWrite<TextStatisticsTag>(),
            };
            return new ComponentTypeSet(result);
        }
        internal static EntityArchetype GetFontStateArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(2, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<FontState>();
            componentTypeStaging[1] = ComponentType.ReadWrite<FontsDirtyTag>(); //initialize Font state to `dirty` to prevent premature system updates
            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }
        internal static EntityArchetype GetNativeFontDataArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(7, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<FontAssetRef>();      // do not copy FontBlobReference for this information as blob pointer will not survive scene reload
            componentTypeStaging[1] = ComponentType.ReadWrite<FontAssetMetadata>(); // do not copy FontBlobReference for this information as blob pointer will not survive scene reload
            componentTypeStaging[2] = ComponentType.ReadWrite<AtlasData>();
            componentTypeStaging[3] = ComponentType.ReadWrite<MissingGlyphs>();
            componentTypeStaging[4] = ComponentType.ReadWrite<UsedGlyphs>();  
            componentTypeStaging[5] = ComponentType.ReadWrite<UsedGlyphRects>();
            componentTypeStaging[6] = ComponentType.ReadWrite<FreeGlyphRects>();
            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }

        public static EntityArchetype GetSingleFontTextArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(17, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<FontBlobReference>();            
            componentTypeStaging[1] = ComponentType.ReadWrite<TextBaseConfiguration>();
            componentTypeStaging[2] = ComponentType.ReadWrite<TextRenderControl>();
            componentTypeStaging[3] = ComponentType.ReadWrite<GlyphOTF>();
            componentTypeStaging[4] = ComponentType.ReadWrite<CalliByte>();
            componentTypeStaging[5] = ComponentType.ReadWrite<XMLTag>();
            componentTypeStaging[6] = ComponentType.ReadWrite<RenderGlyph>();
            componentTypeStaging[7] = ComponentType.ReadWrite<RenderGlyphOld>();
            componentTypeStaging[8] = ComponentType.ReadWrite<TextShaderIndex>();
            componentTypeStaging[9] = ComponentType.ReadWrite<LocalTransform>();
            componentTypeStaging[10] = ComponentType.ReadWrite<LocalToWorld>();
            componentTypeStaging[11] = ComponentType.ReadWrite<WorldToLocal_Tag>();
            componentTypeStaging[12] = ComponentType.ReadWrite<WorldRenderBounds>();
            componentTypeStaging[13] = ComponentType.ReadWrite<RenderBounds>();
            componentTypeStaging[14] = ComponentType.ReadWrite<PerInstanceCullingTag>();
            componentTypeStaging[15] = ComponentType.ReadWrite<MaterialMeshInfo>();
            componentTypeStaging[16] = ComponentType.ReadWrite<RenderFilterSettings>();            

            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }
        public static EntityArchetype GetMultiFontParentTextArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(21, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<FontBlobReference>();
            componentTypeStaging[1] = ComponentType.ReadWrite<TextBaseConfiguration>();
            componentTypeStaging[2] = ComponentType.ReadWrite<TextRenderControl>();
            componentTypeStaging[3] = ComponentType.ReadWrite<GlyphOTF>();
            componentTypeStaging[4] = ComponentType.ReadWrite<CalliByte>();
            componentTypeStaging[5] = ComponentType.ReadWrite<XMLTag>();
            componentTypeStaging[6] = ComponentType.ReadWrite<RenderGlyph>();
            componentTypeStaging[7] = ComponentType.ReadWrite<RenderGlyphOld>();
            componentTypeStaging[8] = ComponentType.ReadWrite<RenderGlyphMask>();
            componentTypeStaging[9] = ComponentType.ReadWrite<TextShaderIndex>();
            componentTypeStaging[10] = ComponentType.ReadWrite<TextMaterialMaskShaderIndex>();
            componentTypeStaging[11] = ComponentType.ReadWrite<FontMaterialSelectorForGlyph>();
            componentTypeStaging[12] = ComponentType.ReadWrite<AdditionalFontMaterialEntity>();
            componentTypeStaging[13] = ComponentType.ReadWrite<LocalTransform>();
            componentTypeStaging[14] = ComponentType.ReadWrite<LocalToWorld>();
            componentTypeStaging[15] = ComponentType.ReadWrite<WorldToLocal_Tag>();
            componentTypeStaging[16] = ComponentType.ReadWrite<WorldRenderBounds>();
            componentTypeStaging[17] = ComponentType.ReadWrite<RenderBounds>();
            componentTypeStaging[18] = ComponentType.ReadWrite<PerInstanceCullingTag>();
            componentTypeStaging[19] = ComponentType.ReadWrite<MaterialMeshInfo>();
            componentTypeStaging[20] = ComponentType.ReadWrite<RenderFilterSettings>();            

            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }
        public static EntityArchetype GetMultiFontChildTextArchetype(ref SystemState state)
        {
            var componentTypeStaging = new NativeArray<ComponentType>(13, Allocator.Temp);
            componentTypeStaging[0] = ComponentType.ReadWrite<FontBlobReference>();            
            componentTypeStaging[1] = ComponentType.ReadWrite<TextRenderControl>();
            componentTypeStaging[2] = ComponentType.ReadWrite<RenderGlyphMask>();
            componentTypeStaging[3] = ComponentType.ReadWrite<TextShaderIndex>();
            componentTypeStaging[4] = ComponentType.ReadWrite<TextMaterialMaskShaderIndex>();
            componentTypeStaging[5] = ComponentType.ReadWrite<LocalTransform>();
            componentTypeStaging[6] = ComponentType.ReadWrite<LocalToWorld>();
            componentTypeStaging[7] = ComponentType.ReadWrite<WorldToLocal_Tag>();
            componentTypeStaging[8] = ComponentType.ReadWrite<WorldRenderBounds>();
            componentTypeStaging[9] = ComponentType.ReadWrite<RenderBounds>();
            componentTypeStaging[10] = ComponentType.ReadWrite<PerInstanceCullingTag>();
            componentTypeStaging[11] = ComponentType.ReadWrite<MaterialMeshInfo>();
            componentTypeStaging[12] = ComponentType.ReadWrite<RenderFilterSettings>();

            return state.EntityManager.CreateArchetype(componentTypeStaging);
        }
    }
}

