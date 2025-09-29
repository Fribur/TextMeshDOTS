#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using TextMeshDOTS.HarfBuzz;
using Unity.Collections;
using Font = TextMeshDOTS.HarfBuzz.Font;

namespace TextMeshDOTS.Authoring
{
    [CreateAssetMenu(fileName = "FontUtility", menuName = "TextMeshDOTS/FontUtility")]

    // Use this utility to get the information requiered for spawning TextRenderer at runtime vai FontRequest. See
    // RuntimeSpawner/RuntimeSingleFontTextRendererSpawner and
    // RuntimeSpawner/RuntimeMultiFontTextRendererSpawner
    // Drag and drop a font object into the font field
    public class FontUtility : ScriptableObject
    {
        public Object font;
        public string fontAssetPath;
        public string fontFamily;
        public string fontSubFamily;
        public string typographicFamily;
        public string typographicSubfamily;
        public int weight;
        public int width;
        public string isItalic;
        public int slant;
        
        public void OnValidate()
        {
            if ((font == null))
                return;

            fontAssetPath = AssetDatabase.GetAssetPath(font);
            bool isTrueType = fontAssetPath.EndsWith("ttf", System.StringComparison.OrdinalIgnoreCase);
            bool isOpentype = fontAssetPath.EndsWith("otf", System.StringComparison.OrdinalIgnoreCase);
            if (isOpentype || isTrueType)
            {
                var fontBytes = File.ReadAllBytes(fontAssetPath);
                Blob blob;
                unsafe
                {
                    fixed (byte* bytes = fontBytes)
                    {
                        blob = new Blob(bytes, (uint)fontBytes.Length, MemoryMode.READONLY);
                    }
                }

                var face = new Face(blob.ptr, 0);
                var font = new Font(face.ptr);

                //fetch name of fontFamily and subFamily, generate hash code from that used to lookup this font
                var language = new Language(Harfbuzz.HB_TAG('E', 'N', 'G', ' '));

                var initialCapacity = 125u; //FixedString128Bytes.Capacity
                var tmp = new FixedString128Bytes();
                uint textSize = initialCapacity;
                face.GetFaceInfo(NameID.FONT_FAMILY, language, ref textSize, ref tmp);
                tmp.Length = (int)textSize;
                fontFamily = tmp.ToString();

                textSize = initialCapacity;
                face.GetFaceInfo(NameID.FONT_SUBFAMILY, language, ref textSize, ref tmp);
                tmp.Length = (int)textSize;
                fontSubFamily = tmp.ToString();

                textSize = initialCapacity;
                face.GetFaceInfo(NameID.TYPOGRAPHIC_FAMILY, language, ref textSize, ref tmp);
                tmp.Length = (int)textSize;
                typographicFamily = tmp.ToString();

                textSize = initialCapacity;
                face.GetFaceInfo(NameID.TYPOGRAPHIC_SUBFAMILY, language, ref textSize, ref tmp);
                tmp.Length = (int)textSize;
                typographicSubfamily = tmp.ToString();

                weight = (int)font.GetStyleTag(StyleTag.WEIGHT);
                width = (int)font.GetStyleTag(StyleTag.WIDTH);
                var italic = (byte)font.GetStyleTag(StyleTag.ITALIC);
                isItalic = italic == 1 ? "true" : "false";
                slant = (int)font.GetStyleTag(StyleTag.SLANT_ANGLE);
                font.Dispose();
                face.Dispose();
                blob.Dispose();
            }
            else
            {
                Debug.LogWarning("Ensure you only have files ending with 'ttf' or 'otf' (case insensitiv) in font list");
                return;
            }
        }
    }
}
#endif