using System;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [Serializable]
    public class SymlinkResourceInfo
    {
        public string sourcePath = string.Empty;
        public string destPath = string.Empty;
        public bool isPackage = false;
        public bool isRelative = false;
        public PackageDirInfo packageLinkInfo = new();
    }
}