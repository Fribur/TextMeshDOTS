using TextMeshDOTS.Rendering;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;


namespace TextMeshDOTS
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [CreateAfter(typeof(EntitiesGraphicsSystem))]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    partial class RunttimeMaterialSpawner : SystemBase
    {
        private const string kUnified_URP_Material = "Unified-URP";

        EntitiesGraphicsSystem hybridRenderer;

        Material runtimeMaterial;
        Mesh backendMesh;
        BatchMeshID backendMeshID;
        BatchMaterialID runtimeMaterialID;

        protected override void OnCreate()
        {
            hybridRenderer = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            backendMesh = Resources.Load<Mesh>(RenderingTools.kTextBackendMeshResource);
            backendMeshID = BatchMeshID.Null;
            var srpType = GraphicsSettings.defaultRenderPipeline.GetType().ToString();
            if (srpType.Contains("HDRenderPipelineAsset"))
            {
                //Debug.Log("High Definition Render Pipeline (HDRP) is being used.");
                //runtimeMaterial = Resources.Load<Material>(TextMaterialUtility.kUnified_HDRP_Material);
            }
            else if (srpType.Contains("UniversalRenderPipelineAsset") || srpType.Contains("LightweightRenderPipelineAsset"))
            {
                //Debug.Log("Universal Render Pipeline (URP) is being used.");
                runtimeMaterial = Resources.Load<Material>(kUnified_URP_Material);
            }
            else
                Debug.LogError("TextMeshDOTS does not work with the Built-in (Legacy) Render Pipeline");

            EntityManager.CreateSingleton(new RuntimeFontMaterial
            {
                batchMaterialID = BatchMaterialID.Null,
                batchMeshID = BatchMeshID.Null,                
            });
        }

        protected override void OnUpdate()
        {
            if (backendMeshID == BatchMeshID.Null && runtimeMaterialID == BatchMaterialID.Null)
            {
                var runtimeFontMaterial = SystemAPI.GetSingletonRW<RuntimeFontMaterial>();
                runtimeFontMaterial.ValueRW.batchMaterialID = hybridRenderer.RegisterMaterial(runtimeMaterial);
                runtimeFontMaterial.ValueRW.batchMeshID = hybridRenderer.RegisterMesh(backendMesh);
                this.Enabled = false;
            }
        }
    }
}