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
        public VerticalAlignmentOptions   verticalAlignment   = VerticalAlignmentOptions.Top;
        public bool                       isOrthographic;
        public bool                       enableKerning       = true;
        public FontStyles                 fontStyle;
        public FontWeight                 fontWeight;

        public Color32 color = Color.white;

        public FontAsset font;
    }


    [TemporaryBakingType]
    internal class TextRendererBaker : Baker<TextRendererAuthoring>
    {
        public override void Bake(TextRendererAuthoring authoring)
        {
            if (authoring.font == null)
                return;            

            var entity = GetEntity(TransformUsageFlags.Renderable);

            //Fonts
            AddFontRendering(entity, authoring.font);            

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
            });

            var textStatsEntity = CreateAdditionalEntity(TransformUsageFlags.None);
            AddComponent(textStatsEntity, TextMeshDOTSArchetypes.GetTextStatisticsTypeset());
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

