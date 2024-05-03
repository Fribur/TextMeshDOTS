using System.Collections.Generic;
using TextMeshDOTS.Rendering;
using TextMeshDOTS.Rendering.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor.SceneManagement;
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
            if (authoring.fonts == null)
                return;            

            var entity = GetEntity(TransformUsageFlags.Renderable);

            //Fonts
            var font = authoring.fonts[0];
            font.ReadFontAssetDefinition();
            AddFontRendering(entity, font);
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
                    AddFontRendering(newEntity, font);
                    AddComponent<TextMaterialMaskShaderIndex>(newEntity);
                    AddBuffer<RenderGlyphMask>(newEntity);
                    additionalEntities.Add(newEntity);
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
                lineSpacing = authoring.lineSpacing,
                paragraphSpacing = authoring.paragraphSpacing,
            });
        }

        void AddFontRendering(Entity entity, FontAsset fontAsset)
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
            this.BakeTextBackendMeshAndMaterial(entity, renderMeshDescription, fontAsset.material);

            var customHash = new Unity.Entities.Hash128((uint)fontAsset.GetHashCode(), 0, 0, 0);
            if (!TryGetBlobAssetReference(customHash, out BlobAssetReference<FontBlob> blobReference))
            {
                blobReference = FontBlobber.BakeFont(fontAsset);

                // Register the Blob Asset to the Baker for de-duplication and reverting.
                AddBlobAssetWithCustomHash<FontBlob>(ref blobReference, customHash);
            }
            AddComponent(entity, new FontBlobReference { blob = blobReference });            
        }
    }
}

