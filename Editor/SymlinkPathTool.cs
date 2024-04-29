using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniGame.Symlinks.Symlinker.Editor
{
    public static class SymlinkPathTool
    {
        
        private static string _projectPath = string.Empty;
        
        public static string ProjectPath {
            get
            {
                if (!string.IsNullOrEmpty(_projectPath))
                    return _projectPath;
                
                var assetsFolderPath = Application.dataPath;
                assetsFolderPath = FixDirectoryPath(assetsFolderPath);
                assetsFolderPath = TrimEndDirectorySeparator(assetsFolderPath);
                
                var projectFolderPath = assetsFolderPath
                    .Replace("Assets", string.Empty);
                
                _projectPath = projectFolderPath;
                
                return _projectPath;
            }
        }
        
        public static string FixDirectoryPath(string path)
        {
            path = TrimEndDirectorySeparator(path);
            path += "/";
            path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }
        
        public static (PackageDirInfo info, bool found) SelectPackage(string path)
        {
            const FileAttributes attrs = FileAttributes.Directory | FileAttributes.ReparsePoint;
            var srcPackageJsonPath = Path.Combine(path, "package.json");
            var packageFound = (File.GetAttributes(path) & attrs) == attrs && File.Exists(srcPackageJsonPath);

            if (!packageFound) return (default, false);

            var packageJsonString = File.ReadAllText(srcPackageJsonPath);
            var packageInfo = JsonUtility.FromJson<PackageInfo>(packageJsonString);

            var package = new PackageDirInfo
            {
                path = new SymlinkPath(),
                packageInfo = packageInfo
            };

            return (package, true);
        }
        
        public static string GetDestFolderPath(string srcPath)
        {
            srcPath = FixDirectoryPath(srcPath);
            var srcFile = srcPath.TrimEnd(Path.DirectorySeparatorChar);

            var packageData = SelectPackage(srcPath);
            var directory = Path.GetFileName(srcFile);

            var defaultTargetPath = packageData.found
                ? GetPackagesFolderPath()
                : SymLinkerAsset.instance.ProjectResourcePath;

            defaultTargetPath = FixDirectoryPath(defaultTargetPath);

            var assetsFolderPath = Application.dataPath;
            var projectFolderPath = assetsFolderPath;
            var packageFolderPath = projectFolderPath + $"/{defaultTargetPath}{directory}/";
            return packageFolderPath.Replace('/', Path.DirectorySeparatorChar);
        }
        
        public static string GetRelativePath(string path)
        {
            path = path.Replace(ProjectPath, string.Empty);
            return path;
        }
        
        public static string GetAbsolutePath(string path)
        {
            if(path.Contains(ProjectPath, StringComparison.OrdinalIgnoreCase))
                return path;
            return PathCombine(ProjectPath, path);
        }
        
        public static string PathCombine(string path1, string path2)
        {
            path1 = TrimEndDirectorySeparator(path1);
            path2 = TrimStartDirectorySeparator(path2);
            var path = path1 + "/" + path2;
            path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }
        
        public static void DeleteLinkedFolderAsset(string path)
        {
            FileUtil.DeleteFileOrDirectory(path);
            var metaPath = TrimEndDirectorySeparator(path);
            metaPath += ".meta";
            FileUtil.DeleteFileOrDirectory(metaPath);
        }
        
        public static string TrimEndDirectorySeparator(string path)
        {
            path = path.TrimEnd('/');
            path = path.TrimEnd('\\');
            return path;
        }
        
        public static string TrimStartDirectorySeparator(string path)
        {
            path = path.TrimStart('/');
            path = path.TrimStart('\\');
            return path;
        }

        public static string GetPackagePath(string path)
        {
            return PathCombine(path, "package.json");
        }
        
        public static string GetPackagesFolderPath()
        {
            var assetsFolderPath = Application.dataPath;
            var projectFolderPath = assetsFolderPath.Substring(0, assetsFolderPath.Length - "/Assets".Length);
            var packageFolderPath = projectFolderPath + "/Packages";
            return packageFolderPath.Replace('/', Path.DirectorySeparatorChar);
        }
        
        public static bool IsRelativePath(string path)
        {
            return path.Contains(ProjectPath, StringComparison.OrdinalIgnoreCase);
        }
        
        public static string ToRelativePath(string path)
        {
            var isRelative = IsRelativePath(path);
            path = isRelative
                ? path.Replace(ProjectPath, string.Empty)
                : path;
            return path;
        }
    }
}