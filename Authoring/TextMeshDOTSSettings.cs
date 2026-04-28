using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TextMeshDOTS.Authoring
{
    /// <summary>
    /// Project-wide settings for TextMeshDOTS. Access via TextMeshDOTSSettings.Loaded.
    /// Configure through Edit > Project Settings > TextMeshDOTS.
    /// </summary>
    public class TextMeshDOTSSettings : ScriptableObject
    {
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
                var guids = AssetDatabase.FindAssets("t:TextMeshDOTSSettings");
                if (guids.Length > 0)
                    s_Loaded = AssetDatabase.LoadAssetAtPath<TextMeshDOTSSettings>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
#else
                s_Loaded = Resources.Load<TextMeshDOTSSettings>("TextMeshDOTSSettings");
#endif
                return s_Loaded;
            }
        }
    }
}
