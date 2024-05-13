using System.Collections.Generic;
using TextMeshDOTS.Rendering;
using TextMeshDOTS.Rendering.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;

namespace TextMeshDOTS.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("TextMeshDOTS/Text Renderer")]
    public class TextRendererAuthoring : MonoBehaviour
    {
        [TextArea(5, 10)]
        public string text;

        public float                      fontSize            = 12f;
        public bool                       wordWrap            = true;
        public float                      maxLineWidth        = float.MaxValue;
        public HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Left;
        public VerticalAlignmentOptions   verticalAlignment   = VerticalAlignmentOptions.TopAscent;
        public bool                       isOrthographic      = false;
        public bool                       enableKerning       = true;
        public FontStyles                 fontStyle           = FontStyles.Normal;
        public FontWeight                 fontWeight          = FontWeight.Regular;
        [Tooltip("Additional word spacing in font units where a value of 1 equals 1/100em.")]
        public float wordSpacing = 0;
        [Tooltip("Additional line spacing in font units where a value of 1 equals 1/100em.")]
        public float lineSpacing = 0;
        [Tooltip("Paragraph spacing in font units where a value of 1 equals 1/100em.")]
        public float paragraphSpacing = 0;

        public Color32 color = Color.white;

        public List<FontAsset> fonts;
    }


    [TemporaryBakingType]
    internal class TextRendererBaker : Baker<TextRendererAuthoring>
    {
        public override void Bake(TextRendererAuthoring authoring)
        {
            if (authoring.fonts == null || authoring.fonts.Count == 0 || authoring.fonts[0] == null)
                return;

            var backEndMesh = Resources.Load<Mesh>(TextBackendBakingUtility.kTextBackendMeshResource);

            //add MeshFilter and MeshRender on main entity to ensure it correctly converted 
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = authoring.gameObject.AddComponent<MeshRenderer>();
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = authoring.gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = backEndMesh;
            meshRenderer.material = authoring.fonts[0].material;

            var entity = GetEntity(TransformUsageFlags.Renderable);            

            //Fonts
            var font = authoring.fonts[0];
            font.ReadFontAssetDefinition();
            BakeFontAsset(entity, font);
            AddBuffer<RenderGlyph>(entity);

            if (authoring.fonts.Count > 1)
            {
                AddComponent<TextMaterialMaskShaderIndex>(entity);
                AddBuffer<FontMaterialSelectorForGlyph>(entity);
                AddBuffer<RenderGlyphMask>(entity);
                var additionalEntities = AddBuffer<Rendering.AdditionalFontMaterialEntity>(entity).Reinterpret<Entity>();
                for (int i = 1, length= authoring.fonts.Count; i <length ; i++)
                {
                    var newEntity = CreateAdditionalEntity(TransformUsageFlags.Renderable);
                    font = authoring.fonts[i];
                    if (font == null)
                        continue;
                    font.ReadFontAssetDefinition();
                    BakeFontAsset(newEntity, font);
                    AddComponent<TextMaterialMaskShaderIndex>(newEntity);
                    AddBuffer<RenderGlyphMask>(newEntity);
                    additionalEntities.Add(newEntity);
                   
                    //add all components MeshRendererBaker would add to a single rendered entity 
                    AddEntityGraphicsComponents(newEntity, font, backEndMesh);
                    //add MeshRendererBakingData to trick RenderMeshPostProcessSystem to process this entity
                    //important for incremental baking to update MaterialMeshInfo
                    this.AddMeshRendererBakingData(newEntity, meshRenderer);
                }
            }

            //Text Content
            var calliString = new CalliString(AddBuffer<CalliByte>(entity));
            calliString.Append(authoring.text);
            AddComponent(entity, new TextBaseConfiguration
            {
                fontSize          = authoring.fontSize,
                color             = authoring.color,
                maxLineWidth      = math.select(float.MaxValue, authoring.maxLineWidth, authoring.wordWrap),
                lineJustification = authoring.horizontalAlignment,
                verticalAlignment = authoring.verticalAlignment,
                isOrthographic    = authoring.isOrthographic,
                enableKerning     = authoring.enableKerning,
                fontStyle         = authoring.fontStyle,
                fontWeight        = authoring.fontWeight,
                wordSpacing = authoring.wordSpacing,
                lineSpacing = authoring.lineSpacing,
                paragraphSpacing = authoring.paragraphSpacing,
            });
        }

        void BakeFontAsset(Entity entity, FontAsset fontAsset)
        {
            DependsOn(fontAsset);
            
            AddComponent(entity, new TextRenderControl { flags = TextRenderControl.Flags.Dirty });
            AddComponent<TextShaderIndex>(entity);

            var customHash = new Unity.Entities.Hash128((uint)fontAsset.GetHashCode(), 0, 0, 0);
            if (!TryGetBlobAssetReference(customHash, out BlobAssetReference<FontBlob> blobReference))
            {
                blobReference = FontBlobber.BakeFont(fontAsset);

                // Register the Blob Asset to the Baker for de-duplication and reverting.
                AddBlobAssetWithCustomHash<FontBlob>(ref blobReference, customHash);
            }
            AddComponent(entity, new FontBlobReference { blob = blobReference });            
        }
        void AddEntityGraphicsComponents(Entity entity, FontAsset fontAsset, Mesh backEndMesh)
        {
            DependsOn(fontAsset);
            var layer = GetLayer();
            var renderMeshDescription = new RenderMeshDescription
            {
                FilterSettings = new RenderFilterSettings
                {
                    Layer = layer,
                    RenderingLayerMask = (uint)(1 << layer),
                    ShadowCastingMode = ShadowCastingMode.Off,
                    ReceiveShadows = false,
                    MotionMode = MotionVectorGenerationMode.Object,
                    StaticShadowCaster = false,
                },
                LightProbeUsage = LightProbeUsage.Off,
            };
            this.BakeMeshAndMaterial(entity, renderMeshDescription, backEndMesh, fontAsset.material);            
        }
    }
}

