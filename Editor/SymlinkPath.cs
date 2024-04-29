﻿using System;
using Newtonsoft.Json;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [Serializable]
    public struct SymlinkPath
    {
        public bool isRelative;
        public string path;

        [Unity.Plastic.Newtonsoft.Json.JsonIgnore]
        [JsonIgnore]
        public string Path => !isRelative ? path : SymlinkPathTool.GetRelativePath(path);
        
        [Unity.Plastic.Newtonsoft.Json.JsonIgnore]
        [JsonIgnore]
        public string AbsolutePath => isRelative ? path : SymlinkPathTool.GetAbsolutePath(path);

        public static SymlinkPath Create(string targetPath)
        {
            var isRelative = SymlinkPathTool.IsRelativePath(targetPath);
            return new SymlinkPath()
            {
                isRelative = isRelative,
                path = isRelative 
                    ? SymlinkPathTool.GetRelativePath(targetPath)
                    : SymlinkPathTool.GetAbsolutePath(targetPath)
            };
        }
    }
}