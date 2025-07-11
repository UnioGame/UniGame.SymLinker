using System;
using Newtonsoft.Json;

namespace UniGame.Symlinks.Editor
{
    [Serializable]
    public struct SymlinkPath
    {
        public bool isRelative;
        public string path;

        [JsonIgnore]
        public string Path => !isRelative ? path : SymlinkPathTool.GetRelativePath(path);
        
        [JsonIgnore]
        public string AbsolutePath => SymlinkPathTool.GetAbsolutePath(path);

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