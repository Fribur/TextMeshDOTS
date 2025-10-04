using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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
        public Material material;
    }

    class TextRendererBaker : Baker<TextRendererAuthoring>
    {
        public override void Bake(TextRendererAuthoring authoring)
        {
            DependsOn(authoring.fontCollectionAsset);
            int fontCount = 0;
            if (authoring.fontCollectionAsset == null || 
                (fontCount = authoring.fontCollectionAsset.fontRequests.Count) == 0 || 
                authoring.defaultFont == string.Empty || 
                authoring.material ==null)
                return;

            var backEndMesh = Resources.Load<Mesh>(RenderingTools.kTextBackendMeshResource);
            
            //add MeshFilter and MeshRender on main entity to ensure it correctly converted 
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = authoring.gameObject.AddComponent<MeshRenderer>();
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = authoring.gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = backEndMesh;
            meshRenderer.material = authoring.material;

            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<TextShaderIndex>(entity);
            var fontItem = authoring.fontCollectionAsset.fontRequests[0];
            AddBuffer<XMLTag>(entity);
            AddBuffer<GlyphOTF>(entity);
            AddBuffer<RenderGlyph>(entity);
            var calliByte = AddBuffer<CalliByte>(entity);
            var calliString = new CalliString(calliByte);
            calliString.Append(authoring.text);
            var textBaseConfiguraton = new TextBaseConfiguration
            {
                defaultFontFamilyHash = TextHelper.GetHashCodeCaseInsensitive(authoring.defaultFont),
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
        } 
    }    
}