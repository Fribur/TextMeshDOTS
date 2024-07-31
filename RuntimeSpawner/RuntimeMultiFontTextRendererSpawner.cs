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
    [DisableAutoCreation]
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

            var fontBlobReferenceEntity = fontEntityQ.GetSingletonEntity();
            var multiFontMaterials = SystemAPI.GetBuffer<MultiFontMaterials>(fontBlobReferenceEntity).ToNativeArray(WorldUpdateAllocator);
            var multiFontBlobReferences = SystemAPI.GetBuffer<MultiFontBlobReferences>(fontBlobReferenceEntity).ToNativeArray(WorldUpdateAllocator);
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
                maxLineWidth = 4,
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
            var text2 = "<font=LiberationSans SDF>Liberation Sans</font> <font=NeutraTextBoldItalicAlt SDF>Neutra Text Bold Italic Alt</font>";

            if (frameCount == 0)
            {
                int count = 10;
                float half = count * 0.5f;
                var factor = 5.0f;

                var entities = EntityManager.CreateEntity(textRenderParentArchetype, count * count, WorldUpdateAllocator);
                var additionalEntitiesArray = CollectionHelper.CreateNativeArray<Entity>(multiFontMaterials.Length - 1, WorldUpdateAllocator); 
                for (int x = 0; x < count; x++)
                {
                    for (int y = 0; y < count; y++)
                    {
                        var entity = entities[x * count + y];
                        
                        var calliByteBuffer = EntityManager.GetBuffer<CalliByte>(entity);
                        var calliString = new CalliString(calliByteBuffer);
                        //string text = i.ToString() + j.ToString();
                        calliString.Append(text2);

                        var localTransform = LocalTransform.FromPosition(new float3((x - half) * factor, (y - half) * factor, 0));
                        EntityManager.SetComponentData(entity, textBaseConfiguration);                        
                        EntityManager.SetComponentData(entity, textRenderControl);
                        EntityManager.SetComponentData(entity, new FontBlobReference { blob = multiFontBlobReferences[0].blob });
                        EntityManager.SetComponentData(entity, localTransform);
                        EntityManager.SetComponentData(entity, materialMeshInfos[0]);
                        EntityManager.SetSharedComponent(entity, filterSettings);

                        for (int m = 1, length = materialMeshInfos.Length; m < length; m++)
                        {                            
                            var child = EntityManager.CreateEntity(textRenderChildArchetype);
                            additionalEntitiesArray[m - 1] = child; 
                            EntityManager.SetComponentData(child, textRenderControl);
                            EntityManager.SetComponentData(child, new FontBlobReference { blob = multiFontBlobReferences[m].blob });
                            EntityManager.SetComponentData(child, localTransform);
                            EntityManager.SetComponentData(child, materialMeshInfos[m]);
                            EntityManager.SetSharedComponent(child, filterSettings);
                        }
                        var additionalEntities = EntityManager.GetBuffer<AdditionalFontMaterialEntity>(entity).Reinterpret<Entity>();
                        additionalEntities.AddRange(additionalEntitiesArray);
                    }
                }
                //Debug.Log("Text spawned");
            }

            //frameCount++;
            initialized = true;
            
        }
    }
}
