using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.Rendering;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities.Graphics;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace TextMeshDOTS.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("TextMeshDOTS/Text Renderer")]
    public class TextRendererAuthoring : MonoBehaviour
    {
        public FontCollectionAsset fontCollectionAsset;
        [Tooltip("Select the default font family for this TextRenderer. Ensure to assign the font collection asset first")]
        public string defaultFont;

        [TextArea(5, 10)]
        public string text;

        [EnumButtons]
        public FontStyles fontStyles = FontStyles.Normal;

        public float fontSize = 12f;
        public Color32 color = Color.white;
        public HorizontalAlignmentOptions horizontalAlignment = HorizontalAlignmentOptions.Left;
        public VerticalAlignmentOptions verticalAlignment = VerticalAlignmentOptions.TopAscent;
        public bool wordWrap = true;
        public float maxLineWidth = 30;
        public bool isOrthographic = false;
        [Tooltip("Additional word spacing in font units where a value of 1 equals 1/100em.")]
        public float wordSpacing = 0;
        [Tooltip("Additional line spacing in font units where a value of 1 equals 1/100em.")]
        public float lineSpacing = 0;
        [Tooltip("Paragraph spacing in font units where a value of 1 equals 1/100em.")]
        public float paragraphSpacing = 0;
    }

    class TextRendererBaker : Baker<TextRendererAuthoring>
    {
        public override void Bake(TextRendererAuthoring authoring)
        {
            DependsOn(authoring.fontCollectionAsset);
            int fontCount = 0;
            if (authoring.fontCollectionAsset == null || (fontCount = authoring.fontCollectionAsset.fontRequests.Count) == 0 || authoring.defaultFont == string.Empty)
                return;

            var layer = GetLayer();

            var renderFilterSettings = new RenderFilterSettings
            {
                Layer = layer,
                RenderingLayerMask = (uint)(1 << layer),
                ShadowCastingMode = ShadowCastingMode.Off,
                ReceiveShadows = false,
                MotionMode = MotionVectorGenerationMode.Object,
                StaticShadowCaster = false,
            };

            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddEntityGraphicsComponents(entity, renderFilterSettings);
            AddComponent(entity, new TextRenderControl { flags = TextRenderControl.Flags.Dirty });
            AddComponent<TextShaderIndexOld>(entity);

            //add for each font in FontCollectionAsset an additional render entity to enable use of this font
            var additionalEntities = new NativeList<Entity>(fontCount, Allocator.Temp);
            for (int i = 0; i < fontCount; i++)
            {
                var fontItem = authoring.fontCollectionAsset.fontRequests[i];
                if (i > 0)
                    AddAdditionalFontEntity(fontItem.fontAssetRef, additionalEntities, renderFilterSettings);
                else
                    AddComponent(entity, new FontBlobReference { value = fontItem.fontAssetRef });
            }

            if (additionalEntities.Length > 0)
            {
                var additionalEntitiesBuffer = AddBuffer<AdditionalFontMaterialEntity>(entity);
                additionalEntitiesBuffer.Reinterpret<Entity>().AddRange(additionalEntities.AsArray());
                AddComponent<TextMaterialMaskShaderIndex>(entity);
                AddBuffer<FontMaterialSelectorForGlyph>(entity);
                AddBuffer<RenderGlyphMask>(entity);
            }

            //Text Content
            AddBuffer<XMLTag>(entity);
            AddBuffer<GlyphOTF>(entity);
            var calliByte = AddBuffer<CalliByte>(entity);
            var calliString = new CalliString(calliByte);
            calliString.Append(authoring.text);
            var textBaseConfiguraton = new TextBaseConfiguration
            {
                defaultFontFamilyHash = TextHelper.GetHashCodeCaseInSensitive(authoring.defaultFont),
                fontSize = (half)authoring.fontSize,
                color = authoring.color,
                maxLineWidth = math.select(float.MaxValue, authoring.maxLineWidth, authoring.wordWrap),
                lineJustification = authoring.horizontalAlignment,
                verticalAlignment = authoring.verticalAlignment,
                isOrthographic = authoring.isOrthographic,
                fontStyles = authoring.fontStyles,
                fontWeight = (authoring.fontStyles & FontStyles.Bold) == FontStyles.Bold ? FontWeight.Bold : FontWeight.Normal,
                fontWidth = FontWidth.Normal, //cannot be set from UI, 
                wordSpacing = (half)authoring.wordSpacing,
                lineSpacing = (half)authoring.lineSpacing,
                paragraphSpacing = (half)authoring.paragraphSpacing,
            };
            AddComponent(entity, textBaseConfiguraton);
            AddBuffer<RenderGlyph>(entity);
            AddBuffer<RenderGlyphOld>(entity);
        }
        
        void AddAdditionalFontEntity(FontAssetRef fontAssetRef, NativeList<Entity> additionalEntities, RenderFilterSettings renderFilterSettings)
        {
            var newEntity = CreateAdditionalEntity(TransformUsageFlags.Renderable);
            AddEntityGraphicsComponents(newEntity, renderFilterSettings);
            AddComponent<TextMaterialMaskShaderIndex>(newEntity);
            AddBuffer<RenderGlyphMask>(newEntity);
            AddComponent(newEntity, new TextRenderControl { flags = TextRenderControl.Flags.Dirty });
            AddComponent<TextShaderIndexOld>(newEntity);

            AddComponent(newEntity, new FontBlobReference { value = fontAssetRef });
            additionalEntities.Add(newEntity);
        }

        //keep in sync with RenderMeshUtility.GenerateComponentTypes
        void AddEntityGraphicsComponents(Entity entity, RenderFilterSettings renderFilterSettings)
        {
            AddComponent<WorldRenderBounds>(entity);
            AddSharedComponent(entity, renderFilterSettings);
            AddComponent<MaterialMeshInfo>(entity); 
            SetComponentEnabled<MaterialMeshInfo>(entity, false); //enable once font texture was generated and registered with BRG
            AddComponent<WorldToLocal_Tag>(entity);
            AddComponent<RenderBounds>(entity);
            AddComponent<PerInstanceCullingTag>(entity);
        }
    }    
}