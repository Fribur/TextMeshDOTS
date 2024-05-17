using TextMeshDOTS.Rendering;
using TextMeshDOTS.Rendering.Authoring;
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
    //[DisableAutoCreation]
    public partial class RuntimeSingleTextRendererSpawner : SystemBase
    {
        bool initialized;
        int frameCount = 0;
        EntityQuery fontEntityQ;
        EntityArchetype textRenderArchetype;
        protected override void OnCreate()
        {
            initialized = false;
            textRenderArchetype = TextMeshDOTSArchetypes.GetSingleFontTextArchetype(ref CheckedStateRef);
            fontEntityQ = new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<FontBlobReference, FontMaterial, BackEndMesh>()
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
            if (fontEntityQ.IsEmptyIgnoreFilter)
                return;

            var fontBlobReferenceEntity = fontEntityQ.GetSingletonEntity();
            var fontMaterial = SystemAPI.GetComponent<FontMaterial>(fontBlobReferenceEntity);            
            var fontBlobReference = SystemAPI.GetComponent<FontBlobReference>(fontBlobReferenceEntity);
            var backEndMesh = SystemAPI.GetComponent<BackEndMesh>(fontBlobReferenceEntity);

            //if (!(frameCount == 0 ^ frameCount == 100))
            if (frameCount != 0)
            {
                frameCount++;
                return;
            }

            var entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var brgMaterialID = entitiesGraphicsSystem.RegisterMaterial(fontMaterial.value);
            var brgMeshID = entitiesGraphicsSystem.RegisterMesh(backEndMesh.value);
            var materialMeshInfo = new MaterialMeshInfo { MaterialID = brgMaterialID, MeshID = brgMeshID };
            var textRenderControl = new TextRenderControl { flags = TextRenderControl.Flags.Dirty };

            var textBaseConfiguration = new TextBaseConfiguration
            {
                fontSize = 12,
                color = (Color32)Color.blue,
                maxLineWidth = 3,
                lineJustification = HorizontalAlignmentOptions.Left,
                verticalAlignment = VerticalAlignmentOptions.TopBase,
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
            //var kerningTest = "WAVES in my Yard YAWN AT MY LAWN Toyota AWAY PALM";



            if (frameCount == 0)
            {
                int count = 100;
                int half = count / 2;
                var factor = 3.0f;
                TextBackendBakingUtility.SetSubMesh(text2.Length, ref materialMeshInfo);
                var entities = EntityManager.CreateEntity(textRenderArchetype, count * count, WorldUpdateAllocator);
                for (int x = 0; x < count; x++)
                {
                    for (int y = 0; y < count; y++)
                    {
                        var entity = entities[x * count + y];
                        EntityManager.SetSharedComponent(entity, filterSettings);
                        var calliByteBuffer = EntityManager.GetBuffer<CalliByte>(entity);
                        var calliString = new CalliString(calliByteBuffer);
                        //string text = i.ToString() + j.ToString();
                        calliString.Append(text2);

                        EntityManager.SetComponentData(entity, textBaseConfiguration);
                        EntityManager.SetComponentData(entity, fontBlobReference);
                        EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3((x - half) * factor, (y - half) * factor, 0)));
                        EntityManager.SetComponentData(entity, textRenderControl);
                        EntityManager.SetComponentData(entity, materialMeshInfo);
                    }
                }
                //Debug.Log("Text spawned");
            }

            if (frameCount == 100)
            {
                int count = 50;
                int half = count / 2;
                var factor = 2.0f;
                textBaseConfiguration.color = Color.red;
                TextBackendBakingUtility.SetSubMesh(text3.Length, ref materialMeshInfo);
                var entities = EntityManager.CreateEntity(textRenderArchetype, count * count, WorldUpdateAllocator);
                for (int x = 0; x < count; x++)
                {
                    for (int y = 0; y < count; y++)
                    {
                        var entity = entities[x * count + y];
                        EntityManager.SetSharedComponent(entity, filterSettings);
                        var calliByteBuffer = EntityManager.GetBuffer<CalliByte>(entity);
                        var calliString = new CalliString(calliByteBuffer);
                        //string text = i.ToString() + j.ToString();
                        calliString.Append(text3);

                        EntityManager.SetComponentData(entity, textBaseConfiguration);
                        EntityManager.SetComponentData(entity, fontBlobReference);
                        EntityManager.SetComponentData(entity, LocalTransform.FromPosition(new float3((x - half) * factor - 1, (y - half) * factor - 1, 0)));
                        EntityManager.SetComponentData(entity, textRenderControl);
                        EntityManager.SetComponentData(entity, materialMeshInfo);
                    }
                }
                //Debug.Log("Text spawned");
            }
            frameCount++;

            //initialized = true;

        }
    }
}
