using TextMeshDOTS;
using TextMeshDOTS.HarfBuzz;
using TextMeshDOTS.HarfBuzz.Bitmap;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Buffer = TextMeshDOTS.HarfBuzz.Buffer;
using ClipType = TextMeshDOTS.Clipper2AoS.ClipType;
using FillRule = TextMeshDOTS.Clipper2AoS.FillRule;
using Font = TextMeshDOTS.HarfBuzz.Font;

#if UNITY_EDITOR
using UnityEditor;
#endif

internal class RenderTest : MonoBehaviour
{
    static readonly ProfilerMarker marker = new ProfilerMarker("Rasterize");
    public UnityEngine.Object sourceFont;
    [SerializeField] private string fontAssetPath;
    public string letter;
    public uint glyphID;
    public int offsetX = 0; 
    public int offsetY = 0;
    public int SPREAD = 8;
    public int padding = 8;
    public int atlasWidth = 1024;
    public int atlasHeight = 1024;
    public int samplingPointSize = 256;
    public bool renderGlyphID;
    public FontAsset fontAsset;
    

    float maxDeviation;
    SDFOrientation orientation;
    public DrawDelegates drawFunctions;
    DrawData drawData;
    PaintDelegates paintFunctions;
    PaintData paintData;
    Blob blob;
    Face face;
    Font font;

    
    void Start()
    {
#if UNITY_EDITOR
        fontAssetPath = AssetDatabase.GetAssetPath(sourceFont);
#endif
        if (fontAssetPath == null)
            return;

        drawFunctions = new DrawDelegates(true);
        paintFunctions = new PaintDelegates(true);
        if (!LoadFont(fontAssetPath, samplingPointSize))
            return;

        //DrawTest(letter, glyphID);
        PaintTest(letter, glyphID); //🌁😉🥰💀✌️🌴🐢🐐🍄⚽🍻👑📸😬👀🚨🏡🕊️🏆😻🌟🧿🍀🎨🍜  

        //var texture = fontAsset.atlasTexture;
        //var texturebuffer = texture.GetPixelData<byte>(0);
        //SDFCommon.WriteArrayToFile("Unity SDF8.txt", texturebuffer, texture.width, texture.height/2);

        //var lang = new Language("en");
        //var script = Script.LATIN;
        //Language.OtTagsFromScriptAndLanguage(script, lang, out NativeList<uint> script_tags, out NativeList<uint> language_tags);

        //foreach (var tag in script_tags)
        //    Debug.Log(Harfbuzz.HB_TAG(tag));

        //foreach (var tag in language_tags)
        //    Debug.Log(Harfbuzz.HB_TAG(tag));
    }


    void Update()
    {
        
    }

    void DrawTest(string character, uint glyphID)
    {
        //shape
        Buffer buffer = default;
        if (!renderGlyphID)
        {
            var language = Language.English;
            buffer = new Buffer(Direction.LTR, Script.LATIN, language);
            //buffer.AddText("😉");
            buffer.AddText(character);
            font.Shape(buffer);
            var glyphInfos = buffer.GetGlyphInfosSpan();
            glyphID = glyphInfos[0].codepoint;
        }

        //get glyph
        drawData = new DrawData(256, 16, maxDeviation, Allocator.Persistent);
        font.DrawGlyph(glyphID, drawFunctions, ref drawData);
        font.GetGlyphExtents(glyphID, out GlyphExtents glyphExtents);

        var atlasRect = glyphExtents.GetPaddedAtlasRect(offsetX, offsetY, padding);

        //allocate texture
        var texture2D = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false);
        var textureData = texture2D.GetRawTextureData<byte>();
        for (int i = 0; i < textureData.Length; i++)
            textureData[i] = 0;                        
        
        //render
         marker.Begin();
        //simplify. Both clipper and polybol outputs the outer contour CCW, and the inner CW, which is postscript definition
        orientation = SDFOrientation.POSTSCRIPT; //clipper always outputs the outer contour CCW, and the inner CW, which is postscript definition
        PolygonOperation.RemoveSelfIntersections(ref drawData, ClipType.Union, FillRule.NonZero);
        //SDFCommon.WriteGlyphOutlineToFile($"Outline of glyph {character}.txt", drawData);
       
        SdfRasterizer.RasterizeSdf8(drawData, textureData, atlasRect, padding, 8);
        //SDFCommon.WriteArrayToFile("TMD SDF32.txt", textureData, texture2D.width, texture2D.height/2);
        marker.End();

        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = texture2D;
        texture2D.Apply();
        if (!renderGlyphID)
            buffer.Dispose();
    }
    
    void PaintTest(string character, uint glyphID)
    {
        var texture2D = new Texture2D(atlasWidth, atlasHeight, TextureFormat.BGRA32, false);
        var textureData = texture2D.GetRawTextureData<ColorBGRA>();
        Blending.SetBlack(textureData);

        Buffer buffer = default;
        //var subpixel_bits = 6;
        if (!renderGlyphID)
        {
            var language = Language.English;
            buffer = new Buffer(Direction.LTR, Script.LATIN, language);
            buffer.AddText(character);
            font.Shape(buffer);
            var glyphInfos = buffer.GetGlyphInfosSpan();
            var glyphPositions = buffer.GetGlyphPositionsSpan();
            glyphID = glyphInfos[0].codepoint;
            //Debug.Log($"glyphID {glyphID} {glyphPositions[0]}");
        }              


        //use harfbuzz rasterizer
        marker.Begin();
        var foreground = new ColorBGRA(0, 0, 0, 255);
        var paint = new Paint(true);
        paint.SetScaleFactor(1, 1);
        paint.SetTransform(1f, 0f, 0f, 1f, 0f, 0f);
        paint.SetForeground(foreground);
        font.GetGlyphExtents(glyphID, out GlyphExtents glyphExtents);
        paint.SetGlyphExtents(ref glyphExtents);

        var pen_x = 0f;
        var pen_y = glyphExtents.height;
        var painted = paint.PaintGlyph(font, glyphID, pen_x, pen_y, 0, foreground);
        if (painted)
        {
            var image = paint.Render();
            image.GetExtents(out RasterExtents rasterExtents);
            var imageBGRA = image.GetColorBGRA(rasterExtents);
            PaintUtils.BlitRawTexture(imageBGRA, (int)rasterExtents.width, (int)rasterExtents.height, textureData, atlasWidth, atlasHeight, 0, 136);
            image.Dispose();
        }
        else
            Debug.Log("Failed to paint");
        marker.End();


        //use custom rasterizer
        paintData = new PaintData(drawFunctions, 256, 4, maxDeviation, Allocator.Temp);
        font.GetGlyphExtents(glyphID, out glyphExtents);
        paintData.clipRect = glyphExtents.ClipRect;
        paintData.clipRect.Expand(1);//prevents rendering artifacts that occur for outlines that strech from minX to maxX of clipRect, reason unknown
        paintData.paintSurface = new NativeArray<ColorBGRA>(paintData.clipRect.intWidth * paintData.clipRect.intHeight, Allocator.Temp);
        //Debug.Log($"clipBox: {paintData.clipRect}");

        marker.Begin();
        font.PaintGlyph(glyphID, ref paintData, paintFunctions, 0, new ColorBGRA(0, 0, 0, 255));
        marker.End();

        switch (paintData.imageFormat)
        {
            case PaintImageFormat.None:
                if (paintData.paintSurface.Length > 0) // rasterized COLR image
                {
                    var clipRect = paintData.clipRect;
                    PaintUtils.BlitRawTexture(paintData.paintSurface, clipRect.intWidth, clipRect.intHeight, textureData, atlasWidth, atlasHeight, 0, 0);
                }
                break;
            case PaintImageFormat.BGRA:
            case PaintImageFormat.PNG:
                if (paintData.paintSurface.Length > 0)
                    PaintUtils.BlitRawTexture(paintData.paintSurface, paintData.imageWidth, paintData.imageHeight, textureData, atlasWidth, atlasHeight, 0, 0);                
                break;
            case PaintImageFormat.SVG:
                break;
        }
        //

        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = texture2D;
        texture2D.Apply(false);
        if (!renderGlyphID)
            buffer.Dispose();
    }

    private void OnDestroy()
    {
        drawFunctions.Dispose();
        drawData.Dispose();
        paintFunctions.Dispose();
        font.Dispose();
        face.Dispose();
        blob.Dispose();
    }
    bool LoadFont(string fontAssetPath, int samplingPointSize)
    {
        if (!TextHelper.IsValidFont(fontAssetPath))
        { 
            Debug.LogWarning("Ensure you only have files ending with 'ttf' or 'otf' (case insensitiv) in font list");
            return false;
        }

        blob = new Blob(fontAssetPath);
        face = new Face(blob, 0);
        font = new Font(face);
        if (face.HasVarData)
        {
            font.VariationNamedInstance = 17; //13 OK, 14 buggy for "6"
            //DisplayVariationAxis();
        }


        //var scale = font.GetScale();        
        font.SetScale(samplingPointSize, samplingPointSize);
        //Debug.Log($"scale: {scale}");

        maxDeviation = BezierMath.GetMaxDeviation(font.GetScale().x);
        //Debug.Log($"Has COLR outlines? {face.HasCOLR()}");
        //Debug.Log($"Has Color Bitmap? {face.HasColorBitmap()}");

        orientation = face.HasTrueTypeOutlines() ? SDFOrientation.TRUETYPE : SDFOrientation.POSTSCRIPT;
        return true;
    }
    void DisplayVariationAxis()
    {
        var axisCount = (int)face.AxisCount;

        //fetch a list of all variation axis
        System.Span<AxisInfo> axisInfos = stackalloc AxisInfo[axisCount];
        face.GetAxisInfos(0, 0, ref axisInfos, out _);
        AxisInfo axisInfo;
        float coord;       

        //fetch a list of named variants                        
        //Debug.Log($"found {axisCount} variation axis for font {fontReference.fontFamily} {fontReference.fontSubFamily}, {face.NamedInstanceCount} named instances");
        System.Span<float> coords = stackalloc float[axisCount];
        for (int k = 0, kk = (int)face.NamedInstanceCount; k < kk; k++)
        {
            Debug.Log($"Named Instance: {k}");
            face.GetNamedInstanceDesignCoords(k, ref coords, out uint coordLength);
            for (int f = 0, ff = (int)coordLength; f < ff; f++)
            {
                //axisInfos and coords should be aligned in length and order
                axisInfo = axisInfos[f];
                coord = coords[f];
                Debug.Log($"Variation axis: {axisInfo.axisTag} {face.GetName(axisInfo.nameID, Language.English)}, value = {coord}");
            }
        }
    }
}