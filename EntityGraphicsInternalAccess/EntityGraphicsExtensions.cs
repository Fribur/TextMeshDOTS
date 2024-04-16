using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;
using System;


public static class EntityGraphicsInternals
{    public static void BakeMeshAndMaterial(this IBaker baker, Entity entity, RenderMeshDescription renderMeshDescription, Mesh mesh,  Material material)
    {
        var componentFlag = RenderMeshUtility.EntitiesGraphicsComponentFlags.Baking;
        componentFlag.AppendMotionAndProbeFlags(renderMeshDescription, baker.IsStatic());
        Material[] materials = new Material[] { material };
        componentFlag.AppendDepthSortedFlag(materials);
        baker.AddComponent(entity, RenderMeshUtility.ComputeComponentTypes(componentFlag));

        var subMeshIndexInfo = new SubMeshIndexInfo32(0);
        baker.SetSharedComponent(entity, renderMeshDescription.FilterSettings);
        baker.SetComponent(entity, new RenderMeshUnmanaged(mesh, material, subMeshIndexInfo));
        baker.SetComponent(entity, new RenderBounds { Value = mesh.bounds.ToAABB() });
    }
}

