using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TextMeshDOTS.Authoring
{
    /// <summary>
    /// Project-wide settings for TextMeshDOTS. Access via TextMeshDOTSSettings.Loaded.
    /// Configure through Edit > Project Settings > TextMeshDOTS.
    /// The asset can live anywhere in the project — no Resources folder required.
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
                // In the editor, find the asset anywhere in the project via GUID search.
                var guids = AssetDatabase.FindAssets("t:TextMeshDOTSSettings");
                if (guids.Length > 0)
                {
                    s_Loaded = AssetDatabase.LoadAssetAtPath<TextMeshDOTSSettings>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
                }

                if (s_Loaded == null)
                    Debug.LogWarning("[TextMeshDOTS] No TextMeshDOTSSettings asset found. Create one via Edit > Project Settings > TextMeshDOTS.");
#else
                // At runtime the asset is already in memory via Preloaded Assets
                // (registered automatically by TextMeshDOTSSettingsPostprocessor).
                // Resources.FindObjectsOfTypeAll searches ALL loaded Unity objects —
                // it is NOT related to the Resources folder and has no path requirement.
                var results = Resources.FindObjectsOfTypeAll<TextMeshDOTSSettings>();
                s_Loaded = results.Length > 0 ? results[0] : null;

                if (s_Loaded == null)
                    Debug.LogError("[TextMeshDOTS] TextMeshDOTSSettings not found at runtime. " +
                                   "Ensure the asset exists and is registered in Player Settings > Preloaded Assets.");
#endif
                return s_Loaded;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Ensures this asset is registered in Player Settings > Preloaded Assets so it is
        /// loaded into memory at runtime without any path-based lookup.
        /// </summary>
        internal void RegisterAsPreloadedAsset()
        {
            var preloaded = PlayerSettings.GetPreloadedAssets();
            foreach (var asset in preloaded)
                if (asset == this) return; // already registered

            var list = new System.Collections.Generic.List<Object>(preloaded) { this };
            PlayerSettings.SetPreloadedAssets(list.ToArray());
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Removes stale null entries and this asset from the Preloaded Assets list.
        /// </summary>
        internal static void UnregisterDestroyedPreloadedAssets()
        {
            var preloaded = PlayerSettings.GetPreloadedAssets();
            var list = new System.Collections.Generic.List<Object>();
            bool changed = false;

            foreach (var asset in preloaded)
            {
                if (asset == null) { changed = true; continue; }
                list.Add(asset);
            }

            if (changed)
                PlayerSettings.SetPreloadedAssets(list.ToArray());
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Automatically keeps TextMeshDOTSSettings registered in Preloaded Assets.
    /// Fires on asset import (including first creation) and cleans up on deletion.
    /// </summary>
    class TextMeshDOTSSettingsPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                var settings = AssetDatabase.LoadAssetAtPath<TextMeshDOTSSettings>(path);
                if (settings != null)
                {
                    settings.RegisterAsPreloadedAsset();
                    return;
                }
            }

            // Clean up null entries left behind by deleted or moved assets.
            if (deletedAssets.Length > 0 || movedFromAssetPaths.Length > 0)
                TextMeshDOTSSettings.UnregisterDestroyedPreloadedAssets();
        }
    }
#endif
}