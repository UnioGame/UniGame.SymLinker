using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniGame.Symlinks.Editor
{
    public static class SymlinkPathTool
    {
        public const string PackageJson = "package.json";
        public const string PackageFolderJson = "Packages";
        
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
        
        public static bool IsPackagePath(string path)
        {
            var srcPackageJsonPath = Path.Combine(path,PackageJson);
            var packageFound = File.Exists(srcPackageJsonPath);
            return packageFound;
        }
        
        public static (PackageDirInfo info, bool found) SelectPackage(string path)
        {
            var packageFound = IsPackagePath(path);

            if (!packageFound) return (default, false);

            var srcPackageJsonPath = Path.Combine(path,PackageJson);
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

            var isPackage = IsPackagePath(srcPath);
            var directory = Path.GetFileName(srcFile);

            var defaultTargetPath = isPackage
                ? GetPackagesFolderPath()
                : PathCombine(Application.dataPath,SymLinkerAsset.instance.ProjectResourcePath);
            
            defaultTargetPath = FixDirectoryPath(defaultTargetPath);
            defaultTargetPath = TrimEndDirectorySeparator(defaultTargetPath);
            
            var resultPath = defaultTargetPath + $"/{directory}/";
            return FixDirectoryPath(resultPath);
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
        
        public static void DeleteFolderWithMeta(string path)
        {
            var folderDeleted = FileUtil.DeleteFileOrDirectory(path);
            var metaPath = TrimEndDirectorySeparator(path);
            metaPath += ".meta";
            var metaDeleted = FileUtil.DeleteFileOrDirectory(metaPath);
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
            var packagePath = Path.Combine(path,PackageJson);
            return packagePath;
        }
        
        public static string GetPackagesFolderPath()
        {
            var packageFolderPath = ProjectPath + "/" + $"{PackageFolderJson}/";
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