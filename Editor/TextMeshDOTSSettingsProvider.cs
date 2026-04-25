using TextMeshDOTS.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TextMeshDOTS.Editor
{
    /// <summary>
    /// Registers the TextMeshDOTS settings page in Edit > Project Settings > TextMeshDOTS.
    /// Auto-creates the settings asset on first open if it doesn't exist.
    /// </summary>
    sealed class TextMeshDOTSSettingsProvider : SettingsProvider
    {
        const string kAssetPath = "Assets/TextMeshDOTSSettings.asset";
        const string kSettingsPath = "Project/TextMeshDOTS";

        TextMeshDOTSSettingsProvider(string path, SettingsScope scope, params string[] keywords)
            : base(path, scope, keywords)
        {
            guiHandler = OnGui;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            EnsureSettingsAssetExists();
        }

        void OnGui(string dummy)
        {
            var settings = TextMeshDOTSSettings.Loaded;
            var serialized = new SerializedObject(settings);
            serialized.Update();

            EditorGUILayout.PropertyField(serialized.FindProperty("useGPURendering"), new GUIContent("GPU Rendering (Slug)"));

            serialized.ApplyModifiedProperties();
        }

        static void EnsureSettingsAssetExists()
        {
            if (AssetDatabase.LoadAssetAtPath<TextMeshDOTSSettings>(kAssetPath) != null)
                return;

            AssetDatabase.Refresh();

            var settings = ScriptableObject.CreateInstance<TextMeshDOTSSettings>();
            AssetDatabase.CreateAsset(settings, kAssetPath);
            AssetDatabase.SaveAssets();
        }

        [SettingsProvider]
        public static SettingsProvider CreateTextMeshDOTSSettingsProvider()
        {
            return new TextMeshDOTSSettingsProvider(kSettingsPath, SettingsScope.Project, "TextMeshDOTS", "Rendering", "Slug");
        }
    }
}
