using TextMeshDOTS.Rendering.Authoring;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace TextMeshDOTS.Authoring
{
    public class FontBlobAuthoring : MonoBehaviour
    {
        public FontAsset font;
    }
    class FontBlobAuthoringBaker : Baker<FontBlobAuthoring>
    {
        public override void Bake(FontBlobAuthoring authoring)
        {
            var spawned = GetEntity(TransformUsageFlags.None);
            var mesh = Resources.Load<Mesh>(LatiosTextBackendBakingUtility.kTextBackendMeshResource);
            AddComponent(spawned, new FontMaterial { fontMaterial = authoring.font.material, backendMesh= mesh });
            authoring.font.ReadFontAssetDefinition();
            var FontBlob = FontBlobber.BakeFont(authoring.font);
            AddComponent(spawned, new FontBlobReference { blob = FontBlob });
            AddBlobAsset(ref FontBlob, out Unity.Entities.Hash128 hash);

            var textStatsEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(textStatsEntity, TextMeshDOTSArchetypes.GetTextStatisticsTypeset());
        }
    }
}

