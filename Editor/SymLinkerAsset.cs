using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [FilePath("UniGame/ResourceSymlinker/SymlinkerAsset.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class SymLinkerAsset : ScriptableSingleton<SymLinkerAsset>
    {
        [Tooltip("Path to linked resources in project")]
        public string ProjectResourcePath = "ExternalAssets/";
        
        [Tooltip("Enable auto link resources on project load")]
        public bool EnableAutoLink = true;
        
        public List<SymlinkResourceInfo> resources = new();
        
        public void Delete(string path)
        {
            var link = FindResource(path);
            if (link != null)
                resources.Remove(link);
        }
        
        public void Delete(SymlinkResourceInfo link)
        {
            resources.Remove(link);
        }
        
        public SymlinkResourceInfo FindResource(string path)
        {
            var absolutePath = SymlinkPathTool.GetAbsolutePath(path);

            foreach (var item in resources)
            {
                var sourcePath = item.sourcePath.AbsolutePath;
                var destPath = item.destPath.AbsolutePath;
                if(sourcePath.Equals(absolutePath, StringComparison.OrdinalIgnoreCase) ||
                   destPath.Equals(absolutePath, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            
            return default;
        }
    }
}