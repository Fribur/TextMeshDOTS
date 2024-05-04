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
using UnityEngine.UIElements;

namespace TextMeshDOTS.Authoring
{
    [BurstCompile]
    public partial class RuntimeMultiFontTextRendererSpawner : SystemBase
    {
        bool initialized;
        int frameCount = 0;
        EntityQuery fontEntityQ;
        EntityArchetype textRenderParentArchetype, textRenderChildArchetype;
        protected override void OnCreate()
        {
            initialized = false;
            textRenderParentArchetype = TextMeshDOTSArchetypes.GetMultiFontParentTextArchetype(ref CheckedStateRef);
            textRenderChildArchetype = TextMeshDOTSArchetypes.GetMultiFontChildTextArchetype(ref CheckedStateRef);
            fontEntityQ = new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<MultiFontBlobReferences, MultiFontMaterials, BackEndMesh>()
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

            var fontBlobReferenceEntities = fontEntityQ.ToEntityArray(Allocator.Temp);
            var fontBlobReferenceEntity = fontBlobReferenceEntities[0];
            var multiFontMaterials = SystemAPI.GetBuffer<MultiFontMaterials>(fontBlobReferenceEntity).ToNativeArray(Allocator.TempJob);
            var multiFontBlobReferences = SystemAPI.GetBuffer<MultiFontBlobReferences>(fontBlobReferenceEntity).ToNativeArray(Allocator.TempJob);
            var backEndMesh = SystemAPI.GetComponent<BackEndMesh>(fontBlobReferenceEntity);
            //if (!(frameCount == 0 ^ frameCount == 100))
            ////if (frameCount != 0)
            //{
            //    frameCount++;
            //    return;
            //}

            var entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();

            var brgMeshID = entitiesGraphicsSystem.RegisterMesh(backEndMesh.value);
            var materialMeshInfos = CollectionHelper.CreateNativeArray<MaterialMeshInfo>(multiFontMaterials.Length, WorldUpdateAllocator);
            for (int i = 0, length= multiFontMaterials.Length; i < length; i++)
            {
                var brgMaterialID = entitiesGraphicsSystem.RegisterMaterial(multiFontMaterials[i].value);
                materialMeshInfos[i] = new MaterialMeshInfo { MaterialID = brgMaterialID, MeshID = brgMeshID };
            }

           
            var textRenderControl = new TextRenderControl { flags = TextRenderControl.Flags.Dirty };
            var textBaseConfiguration = new TextBaseConfiguration
            {
                fontSize = 6,
                color = (Color32)Color.blue,
                maxLineWidth = 10,
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
            var text2 = "Spawner: <font=LiberationSans SDF>LiberationSans</font> <font=NeutraTextBoldItalicAlt SDF>NeutraTextBoldItalicAlt</font>";
            
            if (frameCount == 0)
            {
                int count = 2;
                int half = count / 2;
                var factor = 3.0f;

                var entities = EntityManager.CreateEntity(textRenderParentArchetype, count * count, Allocator.TempJob);
                var additionalEntitiesArray = new NativeArray<Entity>(materialMeshInfos.Length-1, Allocator.TempJob);
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

                        var localTransform = LocalTransform.FromPosition(new float3((x - half) * factor, (y - half) * factor, 0));
                        EntityManager.SetComponentData(entity, textBaseConfiguration);                       
                        EntityManager.SetComponentData(entity, localTransform);
                        EntityManager.SetComponentData(entity, textRenderControl);
                        EntityManager.SetComponentData(entity, materialMeshInfos[0]);
                        EntityManager.SetComponentData(entity, new FontBlobReference { fontBlob = multiFontBlobReferences[0].fontBlob });

                        for (int m = 1, length = materialMeshInfos.Length; m < length; m++)
                        {                            
                            var child = EntityManager.CreateEntity(textRenderChildArchetype);
                            additionalEntitiesArray[m - 1] = child;
                            
                            EntityManager.SetComponentData(child, localTransform);
                            EntityManager.SetComponentData(child, textRenderControl);
                            EntityManager.SetComponentData(child, materialMeshInfos[m]);
                            EntityManager.SetComponentData(child, new FontBlobReference { fontBlob = multiFontBlobReferences[m].fontBlob });
                        }
                        var additionalEntities = EntityManager.GetBuffer<AdditionalFontMaterialEntity>(entity).Reinterpret<Entity>();
                        additionalEntities.AddRange(additionalEntitiesArray);
                    }
                }
                additionalEntitiesArray.Dispose();
                entities.Dispose();
                //Debug.Log("Text spawned");
            }

            //frameCount++;
            multiFontMaterials.Dispose();
            multiFontBlobReferences.Dispose();
            initialized = true;
            
        }
    }
}
