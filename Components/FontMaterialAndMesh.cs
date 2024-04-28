using Unity.Collections;
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

    public struct MulitFontMaterials : IComponentData
    {
        public FixedList128Bytes<UnityObjectRef<Material>> fontMaterials;
        public UnityObjectRef<Mesh> backendMesh;
    }
}
