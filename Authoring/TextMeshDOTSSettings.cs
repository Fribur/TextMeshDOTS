using UnityEngine;

namespace TextMeshDOTS.Authoring
{
    /// <summary>
    /// Project-wide settings for TextMeshDOTS. Access via TextMeshDOTSSettings.Loaded.
    /// Configure through Edit > Project Settings > TextMeshDOTS.
    /// </summary>
    public class TextMeshDOTSSettings : ScriptableObject
    {
#if UNITY_EDITOR
        const string kAssetPath = "Assets/TextMeshDOTSSettings.asset";
#else
        const string kAssetPath = "TextMeshDOTSSettings.asset";
#endif

        [Tooltip("GPU Rendering (Slug): Rasterize glyphs on the GPU using the Slug algorithm. CPU Rendering: Rasterize glyphs on the CPU into texture atlases.")]
        public bool useGPURendering = false;

        static TextMeshDOTSSettings s_Loaded;

        public static TextMeshDOTSSettings Loaded
        {
            get
            {
                if (s_Loaded != null)
                    return s_Loaded;

#if UNITY_EDITOR
                s_Loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<TextMeshDOTSSettings>(kAssetPath);
#else
                s_Loaded = Resources.Load<TextMeshDOTSSettings>("TextMeshDOTSSettings");
#endif
                return s_Loaded;
            }
        }
    }
}
