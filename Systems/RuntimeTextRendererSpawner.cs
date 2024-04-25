using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace TextMeshDOTS.Authoring
{
    [BurstCompile]
    public partial class RuntimeTextRendererSpawner : SystemBase
    {
        bool initialized;
        int frameCount = 0;
        EntityQuery fontEntityQ;
        EntityArchetype textRenderArchetype;
        protected override void OnCreate()
        {
            initialized = false;
            textRenderArchetype = GetTextRenderArchetype(ref CheckedStateRef);
            fontEntityQ = new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<FontBlobReference, FontMaterial>()
                    .Build(EntityManager);
            RequireForUpdate(fontEntityQ);
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnUpdate()
        {
            if (initialized)
                return;

            initialized = true;

            var fontBlobReferenceEntities = fontEntityQ.ToEntityArray(Allocator.Temp);
            var fontBlobReferenceEntity = fontBlobReferenceEntities[0];
            var fontBlobReference = SystemAPI.GetComponent<FontBlobReference>(fontBlobReferenceEntity);
            fontBlobReferenceEntities.Dispose();

            var factor = 3.0f;
            var textBaseConfiguration = new TextBaseConfiguration
            {
                fontSize = 12,
                color = (Color32)Color.white,
                maxLineWidth = 3,
                lineJustification = HorizontalAlignmentOptions.Left,
                verticalAlignment = VerticalAlignmentOptions.Top,
            };
            //var text1 = "the quick brown fox jumps over the lazy dog the quick brown fox jumps over the lazy dog";
            var text2 = "Test 123";
            //var text3 = "ZYX";
            int count = 100;
            int half = count/2;
            if (frameCount == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        var entity = EntityManager.CreateEntity(textRenderArchetype);
                        var calliByteBuffer = EntityManager.GetBuffer<CalliByte>(entity);
                        var calliString = new CalliString(calliByteBuffer);
                        //string text = i.ToString() + j.ToString();
                        calliString.Append(text2);

                        EntityManager.SetComponentData(entity, textBaseConfiguration);
                        EntityManager.SetComponentData(entity, fontBlobReference);
                        EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3((i - half) * factor, (j - half) * factor, 0)));                        
                        EntityManager.SetComponentData(entity, new TextRenderControl { flags = TextRenderControl.Flags.Dirty });
                    }
                }
            }
            frameCount++;
            //Debug.Log("Text spawned");
        }
        public static EntityArchetype GetTextRenderArchetype(ref SystemState state)
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
