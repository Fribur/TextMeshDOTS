using System;
using System.Collections.Generic;
using System.IO;
using TextMeshDOTS.HarfBuzz;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Font = TextMeshDOTS.HarfBuzz.Font;
using Object = UnityEngine.Object;

namespace TextMeshDOTS.Authoring
{
    [CreateAssetMenu(fileName = "FontCollectionAsset", menuName = "TextMeshDOTS/Font Collection Asset")]
    public class FontCollectionAsset : ScriptableObject
    {
        [Tooltip("Drop here Unity Font assets of system font files (.otf .ttf .ttc). Disable \"Include Font Data\" option in these Unity Font assets to ensure fonts are NOT included in your build.")]
        public List<Object> systemFonts;
        [Tooltip("Drop here .otf .ttf .ttc files located in Asset/StreamingAssets(/subfolder)")]
        public List<Object> streamingAssetFonts;
        public List<FontRequest> fontRequests;
        public List<string> fontFamilies;
        public void ProcessFonts()
        {
            Debug.Log("Process Fonts");
            if (fontRequests == null)
                fontRequests = new List<FontRequest>(streamingAssetFonts.Count);
            else
                fontRequests.Clear();

            for (int i = 0, ii = systemFonts.Count; i < ii; i++)
            {
                var fontFile = systemFonts[i];
                if (GetFontInfo(fontFile, true, out FontRequest fontInfo))
                {
                    if(!fontRequests.Contains(fontInfo))
                        fontRequests.Add(fontInfo);
                    else
                    {
                        Debug.LogError($"font {fontFile.name} has been added more than once to the list of fonts");
                        return;
                    }    
                }
                else
                {
                    fontRequests.Clear();
                    Debug.LogError("Error processing system fonts");
                    return;
                }
            }

            for (int i = 0, ii = streamingAssetFonts.Count; i < ii; i++)
            {
                var fontFile = streamingAssetFonts[i];
                if (GetFontInfo(fontFile, false, out FontRequest fontInfo))
                {
                    if (!fontRequests.Contains(fontInfo))
                        fontRequests.Add(fontInfo);
                    else
                    {
                        Debug.LogError($"font {fontFile.name} has been added more than once to the list of fonts");
                        return;
                    }
                }
                else
                {
                    fontRequests.Clear();
                    Debug.LogError("Error processing streamingAsset fonts");
                    return;
                }
            }

            if (fontFamilies == null)
                fontFamilies = new List<string>(fontRequests.Count);
            else
                fontFamilies.Clear();
            for (int i = 0, ii = fontRequests.Count; i < ii; i++)
            {
                var fontInfo = fontRequests[i];
                var fontFamily = fontInfo.typographicFamily == String.Empty ? fontInfo.fontFamily.ToString() : fontInfo.typographicFamily.ToString();
                if (!fontFamilies.Contains(fontFamily))
                    fontFamilies.Add(fontFamily);
            }
            //ensure values are serialized
            EditorUtility.SetDirty(this);
        }
        bool GetFontInfo(Object fontItem, bool useSystemFont, out FontRequest fontInfo)
        {
            fontInfo = new FontRequest();
            fontInfo.useSystemFont = useSystemFont;
            string fontAssetPath = default;
#if UNITY_EDITOR
            fontAssetPath = AssetDatabase.GetAssetPath(fontItem);
#endif
            if (useSystemFont)
                fontInfo.fontAssetPath = string.Empty;
            else
            {
                var pathIndex = fontAssetPath.IndexOf("Assets/StreamingAssets");
                if (pathIndex == -1)
                {
                    fontInfo.streamingAssetLocationValidated = false;
                    Debug.LogWarning($"Unless you want to use System Fonts, the source font asset MUST be in \"Assets/StreamingAssets\"! Font cannot be loaded in a build from current location \"{fontAssetPath}\"");
                    fontInfo.fontAssetPath = Path.GetFullPath(fontAssetPath);
                }
                else
                {
                    fontInfo.streamingAssetLocationValidated = true;
                    fontInfo.fontAssetPath = fontAssetPath.Substring(pathIndex + 23);
                }
            }
            
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
                fontInfo.fontFamily = tmp.ToString();

                textSize = initialCapacity;
                face.GetFaceInfo(NameID.FONT_SUBFAMILY, language, ref textSize, ref tmp);
                tmp.Length = (int)textSize;
                fontInfo.fontSubFamily = tmp.ToString();

                textSize = initialCapacity;
                face.GetFaceInfo(NameID.TYPOGRAPHIC_FAMILY, language, ref textSize, ref tmp);
                tmp.Length = (int)textSize;
                fontInfo.typographicFamily = tmp.ToString();

                textSize = initialCapacity;
                face.GetFaceInfo(NameID.TYPOGRAPHIC_SUBFAMILY, language, ref textSize, ref tmp);
                tmp.Length = (int)textSize;
                fontInfo.typographicSubfamily = tmp.ToString();

                fontInfo.weight = (FontWeight)(byte)(font.GetStyleTag(StyleTag.WEIGHT)/100);
                fontInfo.width = (int)font.GetStyleTag(StyleTag.WIDTH);
                var italic = (byte)font.GetStyleTag(StyleTag.ITALIC);
                fontInfo.isItalic = italic == 1 ? true : false;
                fontInfo.slant = (int)font.GetStyleTag(StyleTag.SLANT_ANGLE);

                //Sampling point size is used to set the font scale.
                //See https://harfbuzz.github.io/harfbuzz-hb-font.html#hb-font-set-scale hardwire for now
                fontInfo.samplingPointSizeSDF = 64;
                fontInfo.samplingPointSizeBitmap = 64;
                fontInfo.fontAssetRef = new FontAssetRef(fontInfo.fontFamily, fontInfo.typographicFamily, fontInfo.weight, fontInfo.width, fontInfo.isItalic, fontInfo.slant);

                font.Dispose();
                face.Dispose();
                blob.Dispose();
                return true;
            }
            else
            {
                Debug.LogWarning("Ensure you only have files ending with 'ttf' or 'otf' (case insensitiv) in font list");
                return false;
            }
        }
    }       
}
