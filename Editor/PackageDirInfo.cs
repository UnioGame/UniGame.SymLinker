#if UNITY_EDITOR

using System;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [Serializable]
    public class PackageDirInfo
    {
        public SymlinkPath path;
        public PackageInfo packageInfo;
    }
}

#endif