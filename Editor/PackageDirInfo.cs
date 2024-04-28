#if UNITY_EDITOR

using System;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [Serializable]
    public class PackageDirInfo
    {
        public string path;
        public PackageInfo packageInfo;
    }
}

#endif