using Unity.Entities;

namespace TextMeshDOTS
{
    struct FontMaterialInstance
    {
        public DynamicBuffer<uint> masks;
        public Aabb aabb;
        public Entity entity;
    }
}

