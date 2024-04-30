using System;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [Serializable]
    public class SymlinkResourceInfo
    {
        public SymlinkPath sourcePath = default;
        public SymlinkPath destPath = default;
        public bool isLinked = false;
        public bool isPackage = false;
        public PackageDirInfo packageLinkInfo = new();
    }
}