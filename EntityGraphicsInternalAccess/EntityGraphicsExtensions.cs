using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;

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
    public static void IndexToQwIndexAndMask(int batchIndex, out int qw, out long mask)
    {
        AtomicHelpers.IndexToQwIndexAndMask(batchIndex, out qw, out mask);
    }
    public static unsafe void AtomicAnd(long* qwords, int index, long value)
    {
        AtomicHelpers.AtomicAnd(qwords, index, value);
    }
    public static void PrintChunkInfo(EntitiesGraphicsChunkInfo info)
    {
        Debug.Log($"Valid {info.Valid} BatchIndex {info.BatchIndex} ChunkTypesBegin {info.ChunkTypesBegin} ChunkTypesEnd {info.ChunkTypesEnd}");
    }
}


