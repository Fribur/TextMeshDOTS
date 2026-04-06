#if UNITY_EDITOR
using TextMeshDOTS.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TextMeshDOTS
{
    [DisallowMultipleComponent]
    [AddComponentMenu("TextMeshDOTS/Font Collection")]
    public class FontCollectionAuthoring : MonoBehaviour
    {
        public FontCollectionAsset fontCollectionAsset;
    }
    class FontCollectionBaker : Baker<FontCollectionAuthoring>
    {
        public override void Bake(FontCollectionAuthoring authoring)
        {
            int fontCount;
            if (authoring.fontCollectionAsset == null || (fontCount = authoring.fontCollectionAsset.fontLoadDescriptions.Count) == 0)
                return;

            var fontLoadDescriptions = new NativeArray<FontLoadDescription>(fontCount, Allocator.Temp);

            var sourceFontLoadDescriptions = authoring.fontCollectionAsset.fontLoadDescriptions;
            for (int i = 0, ii = sourceFontLoadDescriptions.Count; i < ii; i++)
                fontLoadDescriptions[i] = sourceFontLoadDescriptions[i];            

            var entity = GetEntity(TransformUsageFlags.None);
            var fontRequestsBuffer = AddBuffer<FontLoadDescription>(entity);
            fontRequestsBuffer.AddRange(fontLoadDescriptions);
        }        
    }
}
#endif