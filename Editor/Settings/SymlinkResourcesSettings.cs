using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniGame.Symlinks.Symlinker.Editor.Settings
{
    public static class SymlinkResourcesSettings
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/UniGame/Symlink Resources", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "SymLink Resources Settings",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    var settings =  new SerializedObject(SymLinkerAsset.instance);
                    EditorGUILayout.PropertyField(settings.FindProperty(nameof(SymLinkerAsset.ProjectResourcePath)), new GUIContent("Project Linked Resources Path"));
                    EditorGUILayout.PropertyField(settings.FindProperty(nameof(SymLinkerAsset.EnableAutoLink)), new GUIContent("Project Linked Resources Path"));
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Symlink", "Resources","UniGame" })
            };

            return provider;
        }
    }
   
}