using System.Collections.Generic;
using TextMeshDOTS.Rendering;
using TextMeshDOTS.Rendering.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
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
            var mesh = Resources.Load<Mesh>(TextBackendBakingUtility.kTextBackendMeshResource);

            if(authoring.font.Count==1)
            {
                var font = authoring.font[0];
                AddComponent(entity, new BackEndMesh { value = mesh });
                AddComponent(entity, new FontMaterial { value = font.material });
                font.ReadFontAssetDefinition();
                var fontBlob = FontBlobber.BakeFont(font);
                AddComponent(entity, new FontBlobReference { fontBlob = fontBlob });
                AddBlobAsset(ref fontBlob, out Unity.Entities.Hash128 hash);
            }
            else if (authoring.font.Count > 1)
            {
                var multiFontMaterials = new NativeArray<MultiFontMaterials>(authoring.font.Count, Allocator.TempJob);
                var multiFontBlobReferences = new NativeArray<MultiFontBlobReferences>(authoring.font.Count, Allocator.TempJob);
                for (int i = 0; i < authoring.font.Count; i++)
                {
                    var font = authoring.font[i];                    
                    font.ReadFontAssetDefinition();
                    var fontBlob = FontBlobber.BakeFont(font);
                    AddBlobAsset(ref fontBlob, out Unity.Entities.Hash128 hash);
                    multiFontMaterials[i] = new MultiFontMaterials { value = font.material };                    
                    multiFontBlobReferences[i] = new MultiFontBlobReferences { fontBlob = fontBlob };                    
                }

                AddComponent(entity, new BackEndMesh { value = mesh });

                var multiFontMaterialsBuffer = AddBuffer<MultiFontMaterials>(entity);
                multiFontMaterialsBuffer.AddRange(multiFontMaterials);

                var multiFontBlobReferencesBuffer = AddBuffer<MultiFontBlobReferences>(entity);
                multiFontBlobReferencesBuffer.AddRange(multiFontBlobReferences);
                multiFontMaterials.Dispose();
                multiFontBlobReferences.Dispose();
            }
        }
    }
}

