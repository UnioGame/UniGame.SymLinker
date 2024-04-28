using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [Serializable]
    public class ResourceSymLinker
    {
        private string _projectPath = string.Empty;
        
        public List<SymlinkResourceInfo> packages = new();
        public List<SymlinkResourceInfo> folders = new();

        public SymLinkerAsset SymLinkerData => SymLinkerAsset.instance;

        public IReadOnlyList<SymlinkResourceInfo> Packages => packages;

        public IReadOnlyList<SymlinkResourceInfo> Folders => folders;

        public IReadOnlyList<SymlinkResourceInfo> All => SymLinkerData.resources;

        public string ProjectPath {
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
        
        public void EnableAll()
        {
            ReloadLinkedResources();
            RefreshPackageResources();
        }

        public void Refresh()
        {
            RefreshPackageResources();
        }

        public void ReloadLinkedResources()
        {
            var symLinks = SymLinkerData.resources;
            packages.Clear();
            folders.Clear();

            var removedCount = symLinks
                .RemoveAll(x =>
                    !Directory.Exists(x.sourcePath) ||
                    !Directory.Exists(x.destPath));

            var sourceIsChanged = removedCount > 0;

            foreach (var link in symLinks)
            {
                var packageData = SelectPackage(link.sourcePath);
                if (!packageData.found)
                {
                    folders.Add(link);
                    continue;
                }

                packages.Add(link);

                link.isPackage = true;
                link.packageLinkInfo = packageData.info;
                sourceIsChanged = true;
            }

            RefreshPackageResources();

            if (sourceIsChanged)
                EditorUtility.SetDirty(SymLinkerData);
        }

        public (PackageDirInfo info, bool found) SelectPackage(string path)
        {
            const FileAttributes attrs = FileAttributes.Directory | FileAttributes.ReparsePoint;
            var srcPackageJsonPath = Path.Combine(path, "package.json");
            var packageFound = (File.GetAttributes(path) & attrs) == attrs && File.Exists(srcPackageJsonPath);

            if (!packageFound) return (default, false);

            var packageJsonString = File.ReadAllText(srcPackageJsonPath);
            var packageInfo = JsonUtility.FromJson<PackageInfo>(packageJsonString);

            var package = new PackageDirInfo
            {
                path = path,
                packageInfo = packageInfo
            };

            return (package, true);
        }

        public void AddSymlinkResource()
        {
            var srcFolderPath = EditorUtility
                .OpenFolderPanel("Select Source Path", string.Empty, string.Empty);
            AddSymlinkResource(srcFolderPath);
        }
        
        public void RestoreSymLinks()
        {
            foreach (var resource in All)
            {
                RestoreSymLink(resource);
            }
        }

        public void RestoreSymLink(SymlinkResourceInfo link)
        {
            var sourcePath = link.isRelative 
                ? GetRelativePath(link.sourcePath) 
                : link.sourcePath;
            var destPath = link.isRelative 
                ? GetRelativePath(link.destPath) 
                : link.destPath;

            if (Directory.Exists(sourcePath) && Directory.Exists(destPath)) return;
            
            DeleteLinkedFolderAsset(destPath);
            
            if (!Directory.Exists(sourcePath)) return;
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            CreateSymLink(sourcePath, destPath);
        }

        public (SymlinkResourceInfo resource, bool result) CreateSymLink(string srcPath, string dstPath)
        {
            ReloadLinkedResources();

            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"Source path not found: {srcPath}");
                return (null, false);
            }

            var filePath = TrimEndDirectorySeparator(dstPath);
            var dstParent = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dstParent) && !string.IsNullOrEmpty(dstParent))
                Directory.CreateDirectory(dstParent);

#if UNITY_EDITOR_WIN
            var command = $"mklink /j \"{dstPath}\" \"{srcPath}\"";
#else
            var command = $"ln -s {srcPath} {dstPath}";
#endif
            
            var isRelative = IsRelativePath(srcPath);
            if (isRelative)
            {
                srcPath = ToRelativePath(srcPath);
                dstPath = ToRelativePath(dstPath);
            }
            
            var resource = new SymlinkResourceInfo
            {
                sourcePath = srcPath,
                destPath = dstPath,
                isRelative = isRelative,
            };

            if (TryExecuteCmd(command, out _, out var error) != 0)
            {
                //on osx return code can be not 0
                //double check error
                if (string.IsNullOrEmpty(error)) return (resource, true);

                Debug.LogError($"Failed to link package: {error} src {srcPath} dst {dstPath}");
                return (null, false);
            }
            
            return (resource, true);
        }

        public bool IsRelativePath(string path)
        {
            return path.Contains(ProjectPath, StringComparison.OrdinalIgnoreCase);
        }
        
        public string ToRelativePath(string path)
        {
            var isRelative = IsRelativePath(path);
            path = isRelative
                ? path.Replace(ProjectPath, string.Empty)
                : path;
            return path;
        }

        public string GetRelativePath(string path)
        {
            return PathCombine(ProjectPath, path);
        }

        public bool IsSymLink(string path)
        {
            var isAlreadyExists = All.FirstOrDefault(x =>
                x.sourcePath.Equals(path, StringComparison.OrdinalIgnoreCase) ||
                x.destPath.Equals(path, StringComparison.OrdinalIgnoreCase));
            return isAlreadyExists != null;
        }

        private void AddSymlinkResource(string srcFolderPath)
        {
            if (string.IsNullOrEmpty(srcFolderPath))
            {
                Debug.LogError("No folder selected");
                return;
            }

            srcFolderPath = FixDirectoryPath(srcFolderPath);

            if (IsSymLink(srcFolderPath))
            {
                Debug.LogError("Resource already linked");
                return;
            }

            var packageData = SelectPackage(srcFolderPath);
            var destFolderPath = GetDestFolderPath(srcFolderPath);
            destFolderPath = FixDirectoryPath(destFolderPath);

            var symlinkResult = CreateSymLink(srcFolderPath, destFolderPath);

            var symlinkResource = symlinkResult.resource;
            if (!symlinkResult.result)
            {
                Debug.LogError($"Failed to create symlink for {srcFolderPath} to {destFolderPath}");
                return;
            }

            if (packageData.found)
            {
                symlinkResource.isPackage = true;
                symlinkResource.packageLinkInfo = packageData.info;
            }

            SymLinkerData.resources.Add(symlinkResource);

            if (packageData.found) Client.Resolve();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            ReloadLinkedResources();
        }

        public string TrimEndDirectorySeparator(string path)
        {
            path = path.TrimEnd('/');
            path = path.TrimEnd('\\');
            return path;
        }
        
        public string TrimStartDirectorySeparator(string path)
        {
            path = path.TrimStart('/');
            path = path.TrimStart('\\');
            return path;
        }

        public string FixDirectoryPath(string path)
        {
            path = TrimEndDirectorySeparator(path);
            path += "/";
            path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }

        public string PathCombine(string path1, string path2)
        {
            path1 = TrimEndDirectorySeparator(path1);
            path2 = TrimStartDirectorySeparator(path2);
            var path = path1 + "/" + path2;
            path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }

        public SymlinkResourceInfo Find(string path)
        {
            var resource = All.FirstOrDefault(x =>
                x.sourcePath.Equals(path, StringComparison.OrdinalIgnoreCase) ||
                x.destPath.Equals(path, StringComparison.OrdinalIgnoreCase));
            return resource;
        }

        public void DeleteResourceLink(string path)
        {
            var resource = Find(path);
            if (resource == null) return;
            DeleteResourceLink(resource);
        }

        public void DeleteResourceLink(SymlinkResourceInfo into)
        {
            var path = into.destPath;

#if UNITY_EDITOR_WIN
            var command = $"rd \"{path}\"";
#else
            var command = $"unlink {path}";
#endif

            if (TryExecuteCmd(command, out _, out var error) != 0)
            {
                //on osx return code can be not 0
                //double check error
                if (string.IsNullOrEmpty(error) == false)
                {
                    Debug.LogError($"Failed to delete package link: {error}");
                    return;
                }
            }

            DeleteLinkedFolderAsset(path);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            ReloadLinkedResources();
        }

        public void DeleteLinkedFolderAsset(string path)
        {
            FileUtil.DeleteFileOrDirectory(path);
            var metaPath = TrimEndDirectorySeparator(path);
            metaPath += ".meta";
            FileUtil.DeleteFileOrDirectory(metaPath);
        }
        
        public string GetDestFolderPath(string srcPath)
        {
            srcPath = FixDirectoryPath(srcPath);
            var srcFile = srcPath.TrimEnd(Path.DirectorySeparatorChar);

            var packageData = SelectPackage(srcPath);
            var directory = Path.GetFileName(srcFile);

            var defaultTargetPath = packageData.found
                ? GetPackagesFolderPath()
                : SymLinkerData.ProjectResourcePath;

            defaultTargetPath = FixDirectoryPath(defaultTargetPath);

            var assetsFolderPath = Application.dataPath;
            var projectFolderPath = assetsFolderPath;
            var packageFolderPath = projectFolderPath + $"/{defaultTargetPath}{directory}/";
            return packageFolderPath.Replace('/', Path.DirectorySeparatorChar);
        }

        public static string GetPackagesFolderPath()
        {
            var assetsFolderPath = Application.dataPath;
            var projectFolderPath = assetsFolderPath.Substring(0, assetsFolderPath.Length - "/Assets".Length);
            var packageFolderPath = projectFolderPath + "/Packages";
            return packageFolderPath.Replace('/', Path.DirectorySeparatorChar);
        }

        public static int TryExecuteCmd(string command, out string output, out string error)
        {
#if UNITY_EDITOR_WIN
            var cmd = "cmd.exe";
            var args = $"/c {command}";
#else
            var cmd = "/bin/bash";
            var args = $"-c \"{command}\"";
#endif
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage),
                StandardErrorEncoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage),
            };

            var launchProcess = Process.Start(startInfo);
            if (launchProcess == null || launchProcess.HasExited || launchProcess.Id == 0)
            {
                output = error = string.Empty;
                return int.MinValue;
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            launchProcess.OutputDataReceived += (sender, e) => outputBuilder.AppendLine(e.Data ?? "");
            launchProcess.ErrorDataReceived += (sender, e) => errorBuilder.AppendLine(e.Data ?? "");

            launchProcess.BeginOutputReadLine();
            launchProcess.BeginErrorReadLine();
            launchProcess.EnableRaisingEvents = true;

            launchProcess.WaitForExit();

            output = outputBuilder.ToString();
            error = errorBuilder.ToString();
            return launchProcess.ExitCode;
        }

        public void RefreshPackageResources()
        {
            foreach (var resourceInfo in SymLinkerData.resources)
            {
                if (!resourceInfo.isPackage) continue;
                var path = resourceInfo.sourcePath;
                var packageJsonPath = Path.Combine(path, "package.json");

                if (!File.Exists(packageJsonPath))
                    continue;

                var linkInfo = resourceInfo.packageLinkInfo;
                var packageJsonString = File.ReadAllText(packageJsonPath);

                var packageInfo = JsonUtility.FromJson<PackageInfo>(packageJsonString);
                linkInfo.packageInfo = packageInfo;
            }
        }

        public bool IsPackageLinked(string packageName)
        {
            foreach (var dir in All)
            {
                var linkInfo = dir.packageLinkInfo;
                if (linkInfo.packageInfo.name == packageName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}