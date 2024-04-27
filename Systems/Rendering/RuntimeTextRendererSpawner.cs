using TextMeshDOTS.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

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
            textRenderArchetype = TextMeshDOTSArchetypes.GetTextRenderArchetype(ref CheckedStateRef);
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
            if (!SystemAPI.TryGetSingleton(out FontMaterial fontMaterial))
                return;

            if (!(frameCount == 0 ^ frameCount == 100))
            //if (frameCount != 0)
            {
                frameCount++;
                return;
            }

            var entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var brgMaterialID = entitiesGraphicsSystem.RegisterMaterial(fontMaterial.fontMaterial);
            var brgMeshID = entitiesGraphicsSystem.RegisterMesh(fontMaterial.backendMesh);
            var materialMeshInfo = new MaterialMeshInfo { MaterialID = brgMaterialID, MeshID = brgMeshID };
            var textRenderControl = new TextRenderControl { flags = TextRenderControl.Flags.Dirty };

            var fontBlobReferenceEntities = fontEntityQ.ToEntityArray(Allocator.Temp);
            var fontBlobReferenceEntity = fontBlobReferenceEntities[0];
            var fontBlobReference = SystemAPI.GetComponent<FontBlobReference>(fontBlobReferenceEntity);
            fontBlobReferenceEntities.Dispose();

            var textBaseConfiguration = new TextBaseConfiguration
            {
                fontSize = 12,
                color = (Color32)Color.blue,
                maxLineWidth = 3,
                lineJustification = HorizontalAlignmentOptions.Left,
                verticalAlignment = VerticalAlignmentOptions.Top,
            };
            var layer = 1;
            var filterSettings = new RenderFilterSettings
            {
                Layer = layer,
                RenderingLayerMask = (uint)(1 << layer),
                ShadowCastingMode = ShadowCastingMode.Off,
                ReceiveShadows = false,
                MotionMode = MotionVectorGenerationMode.ForceNoMotion,
                StaticShadowCaster = false,
            };

            //var text1 = "the quick brown fox jumps over the lazy dog the quick brown fox jumps over the lazy dog";
            var text2 = "Test 123";
            var text3 = "ZYX";
            int count = 100;
            int half = count / 2;
            var factor = 3.0f;

            var finalText = text2;
            //Debug.Log($"length: {finalText.Length}");
            StaticHelper.SetSubMesh(finalText.Length, ref materialMeshInfo);
            if (frameCount == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        var entity = EntityManager.CreateEntity(textRenderArchetype);
                        EntityManager.SetSharedComponent(entity, filterSettings);
                        var calliByteBuffer = EntityManager.GetBuffer<CalliByte>(entity);
                        var calliString = new CalliString(calliByteBuffer);
                        //string text = i.ToString() + j.ToString();
                        calliString.Append(finalText);

                        EntityManager.SetComponentData(entity, textBaseConfiguration);
                        EntityManager.SetComponentData(entity, fontBlobReference);
                        EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3((i - half) * factor, (j - half) * factor, 0)));
                        EntityManager.SetComponentData(entity, textRenderControl);
                        EntityManager.SetComponentData(entity, materialMeshInfo);
                    }
                }
            }

            count = 50;
            half = count / 2;
            factor = 2.0f;
            textBaseConfiguration.color = Color.red;
            if (frameCount == 100)
            {
                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < count; j++)
                    {
                        var entity = EntityManager.CreateEntity(textRenderArchetype);
                        EntityManager.SetSharedComponent(entity, filterSettings);
                        var calliByteBuffer = EntityManager.GetBuffer<CalliByte>(entity);
                        var calliString = new CalliString(calliByteBuffer);
                        //string text = i.ToString() + j.ToString();
                        calliString.Append(text3);

                        EntityManager.SetComponentData(entity, textBaseConfiguration);
                        EntityManager.SetComponentData(entity, fontBlobReference);
                        EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3((i - half) * factor - 1, (j - half) * factor - 1, 0)));
                        EntityManager.SetComponentData(entity, textRenderControl);
                        EntityManager.SetComponentData(entity, materialMeshInfo);
                    }
                }
            }
            frameCount++;

            //initialized = true;
            //Debug.Log("Text spawned");
        }
    }
}
