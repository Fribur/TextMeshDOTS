using System.Collections.Generic;
using TextMeshDOTS.Rendering;
using TextMeshDOTS.Rendering.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace TextMeshDOTS.Authoring
{
    public class FontBlobAuthoring : MonoBehaviour
    {
        public List<FontAsset> font;
    }
    class FontBlobAuthoringBaker : Baker<FontBlobAuthoring>
    {
        public override void Bake(FontBlobAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var mesh = Resources.Load<Mesh>(LatiosTextBackendBakingUtility.kTextBackendMeshResource);

            if(authoring.font.Count==1)
            {
                var font = authoring.font[0];
                AddComponent(entity, new FontMaterial { fontMaterial = font.material, backendMesh = mesh });
                font.ReadFontAssetDefinition();
                var FontBlob = FontBlobber.BakeFont(font);
                AddComponent(entity, new FontBlobReference { blob = FontBlob });
                AddBlobAsset(ref FontBlob, out Unity.Entities.Hash128 hash);
            }
            else if (authoring.font.Count > 1)
            {
                MulitFontMaterials mulitFontMaterials = new() { backendMesh = mesh};
                MultiFontBlobReferences multiFontBlobReferences = new();
                for (int i = 0; i < authoring.font.Count; i++)
                {
                    var font = authoring.font[i];
                    mulitFontMaterials.fontMaterials[i] = font.material;                    
                    font.ReadFontAssetDefinition();
                    var FontBlob = FontBlobber.BakeFont(font);
                    multiFontBlobReferences.blobs[i]= FontBlob;
                    AddBlobAsset(ref FontBlob, out Unity.Entities.Hash128 hash);
                }
                AddComponent(entity, mulitFontMaterials);
                AddComponent(entity, multiFontBlobReferences);
            }
        }
    }
}

