using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;

public static class EntityGraphicsInternals
{
    //copied from Entitie Graphics MeshRendererBakingUtility.ConvertToSingleEntity
    public static void BakeMeshAndMaterial(this IBaker baker, Entity entity, RenderMeshDescription renderMeshDescription, Mesh mesh,  Material material)
    {
        Material[] materials = new Material[] { material };
        var componentFlag = RenderMeshUtility.EntitiesGraphicsComponentFlags.Baking;
        componentFlag.AppendMotionAndProbeFlags(renderMeshDescription, baker.IsStatic());        
        componentFlag.AppendDepthSortedFlag(materials);
        baker.AddComponent(entity, RenderMeshUtility.ComputeComponentTypes(componentFlag));

        var subMeshIndexInfo = new SubMeshIndexInfo32(0);
        baker.SetSharedComponent(entity, renderMeshDescription.FilterSettings);
        baker.SetComponent(entity, new RenderMeshUnmanaged(mesh, material, subMeshIndexInfo));
        baker.SetComponent(entity, new RenderBounds { Value = mesh.bounds.ToAABB() });
    }
    public static int NumFramesInFlight()
    {
        return SparseUploader.NumFramesInFlight;
    }
}


