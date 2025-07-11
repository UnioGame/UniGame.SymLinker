#if UNITY_EDITOR

using System;

namespace UniGame.Symlinks.Editor
{
    [Serializable]
    public class PackageDirInfo
    {
        public SymlinkPath path;
        public PackageInfo packageInfo;
    }
}

#endif