using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniGame.Symlinks.Symlinker.Editor
{
    public class ResourceSymLinkerWindow : EditorWindow
    {
        #region static data
        
        public const string Title = "Symlink Resources";

        [MenuItem("Tools/Resource Symlinker")]
        public static void OpenWindow()
        {
            var window = GetWindow<ResourceSymLinkerWindow>();
            window.titleContent = new GUIContent(Title);
            window.Show();
        }
        
        #endregion
        
        public ResourceSymLinker symLinker = new();
        public Vector2 scroll;
        public List<SymlinkResourceInfo> resources = new();
        
        private void OnEnable()
        {
            symLinker.ReloadLinkedResources();
        }

        private void OnFocus()
        {
            symLinker.ReloadLinkedResources();
        }

        private void OnGUI()
        {
            OnToolbarGUI();

            scroll = GUILayout.BeginScrollView(scroll, false, true);
            
            OnLinkedResourcesGUI();
            GUILayout.Space(50);
            EditorGUILayout.EndScrollView();
        }
        
        private void OnToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Space(5);

            if (GUILayout.Button("Link Resource", EditorStyles.toolbarButton))
            {
                symLinker.AddSymlinkResource();
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        private void OnLinkedResourcesGUI()
        {
            var labelStyle = EditorStyles.boldLabel;
            labelStyle.richText = true;

            resources.Clear();
            resources.AddRange(symLinker.SymLinkerData.resources);
            
            if (resources.Count == 0)
            {
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No symbolic resources linked in project", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(20);
                
                return;
            }

            GUILayout.Label("Linked packages", EditorStyles.largeLabel);

            foreach (var symLink in resources)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);

                if (GUILayout.Button("Restore ALL Links"))
                {
                    symLinker.RestoreSymLinks();
                }

                var destFilePath = symLinker.TrimEndDirectorySeparator(symLink.destPath);
                var isPackage = symLink.isPackage;
                var packageLink = symLink.packageLinkInfo;
                var label = isPackage
                    ? $"{packageLink.packageInfo.name} : <i>{packageLink.packageInfo.version}</i>"
                    : Path.GetFileNameWithoutExtension(destFilePath);
                
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, labelStyle);
                
                if (GUILayout.Button("Delete link", GUILayout.Width(120)))
                {
                    symLinker.DeleteResourceLink(symLink);
                }
                if (GUILayout.Button("Restore link", GUILayout.Width(120)))
                {
                    symLinker.DeleteResourceLink(symLink);
                }

                GUILayout.EndHorizontal();

                GUILayout.Label($"{nameof(symLink.isRelative)} : {symLink.isRelative}", EditorStyles.miniLabel);
                GUILayout.Label($"{nameof(symLink.isPackage)} : {symLink.isPackage}", EditorStyles.miniLabel);
                GUILayout.Label(symLink.destPath, EditorStyles.miniLabel);

                GUILayout.EndVertical();
            }
        }
        
    }
}