using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace TextMeshDOTS
{
    public struct FontMaterial : IComponentData
    {
        public UnityObjectRef<Material> fontMaterial;
        public UnityObjectRef<Mesh> backendMesh;
    }
}
