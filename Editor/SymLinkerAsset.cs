using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniGame.Symlinks.Editor
{
    [FilePath("ProjectSettings/UniGame/SymLinker/SymLinker.Data.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SymLinkerAsset : ScriptableSingleton<SymLinkerAsset>
    {
        [Tooltip("Path to linked resources in project")]
        public string ProjectResourcePath = "ExternalAssets/";
        
        [Tooltip("Enable auto link resources on project load")]
        public bool EnableAutoLink = true;
        
        public List<SymlinkResourceInfo> resources = new();

        public void Save()
        {
            Save(true);
        }
        
        public bool Add(SymlinkResourceInfo link)
        {
            var exists = FindResource(link.destPath.AbsolutePath) != null;
            if (exists) return false;
            
            resources.Add(link);
            
            Save(true);
            return true;
        }

        public bool CanBeLinked(SymlinkResourceInfo link)
        {
            var sourcePath = link.sourcePath;
            var destPath = link.destPath;
            return Directory.Exists(sourcePath.AbsolutePath) &&
                   !Directory.Exists(destPath.AbsolutePath);
        }
        
        public bool IsValidLink(SymlinkResourceInfo link)
        {
            var sourcePath = link.sourcePath;
            var destPath = link.destPath;
            return Directory.Exists(sourcePath.AbsolutePath) &&
                   Directory.Exists(destPath.AbsolutePath);
        }
        
        public bool Delete(string path)
        {
            var link = FindResource(path);
            return link != null && Delete(link);
        }
        
        public bool Delete(SymlinkResourceInfo link)
        {
            var result = resources.Remove(link);
            if (!result) return false;
            Save(true);
            return true;
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