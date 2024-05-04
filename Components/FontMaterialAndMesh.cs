using Unity.Entities;
using UnityEngine;

namespace TextMeshDOTS
{
    public struct FontMaterial : IComponentData
    {
        public UnityObjectRef<Material> value;
    }

    [InternalBufferCapacity(2)]
    public struct MultiFontMaterials : IBufferElementData
    {
        public UnityObjectRef<Material> value;
    }
    public struct BackEndMesh : IComponentData
    {
        public UnityObjectRef<Mesh> value;
    }
}
