using Unity.Entities;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace TextMeshDOTS.Authoring
{
    public class FontBlobAuthoring : MonoBehaviour
    {
        public FontAsset fontAsset;
    }
    class FontBlobAuthoringBaker : Baker<FontBlobAuthoring>
    {
        public override void Bake(FontBlobAuthoring authoring)
        {
            var spawned = GetEntity(TransformUsageFlags.None);
            AddComponent(spawned, new FontMaterial { fontMaterial = authoring.fontAsset.material });
            authoring.fontAsset.ReadFontAssetDefinition();
            var FontBlob = FontBlobber.BakeFont(authoring.fontAsset);
            AddComponent(spawned, new FontBlobReference { blob = FontBlob });
            AddBlobAsset(ref FontBlob, out Unity.Entities.Hash128 hash);
        }
    }
}

