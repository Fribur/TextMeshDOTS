using System.Collections.Generic;
using TextMeshDOTS.Rendering.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace TextMeshDOTS.Authoring
{
    public class FontBlobAuthoring : MonoBehaviour
    {
        public List<FontAsset> fonts;
    }
    class FontBlobAuthoringBaker : Baker<FontBlobAuthoring>
    {
        public override void Bake(FontBlobAuthoring authoring)
        {
            if (authoring.fonts == null || authoring.fonts.Count == 0 || authoring.fonts[0] == null)
                return;

            var entity = GetEntity(TransformUsageFlags.None);
            var mesh = Resources.Load<Mesh>(TextBackendBakingUtility.kTextBackendMeshResource);

            if(authoring.fonts.Count == 1)
            {
                var font = authoring.fonts[0];
                AddComponent(entity, new BackEndMesh { value = mesh });
                AddComponent(entity, new FontMaterial { value = font.material });
                font.ReadFontAssetDefinition();
                var fontBlob = FontBlobber.BakeFont(font);
                AddComponent(entity, new FontBlobReference { blob = fontBlob });
                AddBlobAsset(ref fontBlob, out Unity.Entities.Hash128 hash);
            }
            else if (authoring.fonts.Count > 1)
            {
                var multiFontMaterials = new NativeArray<MultiFontMaterials>(authoring.fonts.Count, Allocator.TempJob);
                var multiFontBlobReferences = new NativeArray<MultiFontBlobReferences>(authoring.fonts.Count, Allocator.TempJob);
                for (int i = 0; i < authoring.fonts.Count; i++)
                {
                    var font = authoring.fonts[i];
                    if (font == null)
                        continue;
                    font.ReadFontAssetDefinition();
                    var fontBlob = FontBlobber.BakeFont(font);
                    AddBlobAsset(ref fontBlob, out Unity.Entities.Hash128 hash);
                    multiFontMaterials[i] = new MultiFontMaterials { value = font.material };                    
                    multiFontBlobReferences[i] = new MultiFontBlobReferences { blob = fontBlob };                    
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

