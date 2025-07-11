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

namespace UniGame.Symlinks.Editor
{
    [Serializable]
    public class ResourceSymLinker
    {
        public SymLinkerAsset ResourceLinker => SymLinkerAsset.instance;

        public IReadOnlyList<SymlinkResourceInfo> All => ResourceLinker.resources;

        public SymlinkResourceInfo Find(string path)
        {
            return ResourceLinker.FindResource(path);
        }

        public SymlinkResourceInfo CreateLink(string sourcePath)
        {
            var destPath = SymlinkPathTool.GetDestFolderPath(sourcePath);
            
            var symLink = new SymlinkResourceInfo()
            {
                sourcePath = SymlinkPath.Create(sourcePath),
                destPath = SymlinkPath.Create(destPath),
            };

            UpdatePackageInfo(symLink);

            return symLink;
        }

        public void AddSymlinkResource()
        {
            var srcFolderPath = EditorUtility
                .OpenFolderPanel("Select Source Path", string.Empty, string.Empty);
            AddSymlinkResource(srcFolderPath);
        }

        public void RestoreSymLinks()
        {
            var resources = ResourceLinker.resources.ToList();
            foreach (var resource in resources)
                RestoreSymLink(resource);
            
            ResourceLinker.Save();
        }

        public void RestoreSymLink(SymlinkResourceInfo link)
        {
            UpdatePackageInfo(link);
            
            switch (link.isLinked)
            {
                case true when ResourceLinker.IsValidLink(link):
                    return;
                case false:
                    UnlinkResource(link);
                    break;
                case true:
                    CreateSymLink(link);
                    break;
            }
        }

        public bool CreateSymLink(SymlinkResourceInfo info)
        {
            UpdatePackageInfo(info);
            
            info.isLinked = ResourceLinker.IsValidLink(info);
            if(info.isLinked) return false;
            
            var srcPath = info.sourcePath.AbsolutePath;
            var destPath = info.destPath.AbsolutePath;

            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"Source path not found: {srcPath}");
                return false;
            }

            if (Directory.Exists(destPath))
            {
                Debug.LogError($"Dest path already exists {destPath}");
                return false;
            }

            var filePath = SymlinkPathTool.TrimEndDirectorySeparator(destPath);
            var dstParent = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dstParent) && !Directory.Exists(dstParent))
                Directory.CreateDirectory(dstParent);

#if UNITY_EDITOR_WIN
            var command = $"mklink /j \"{destPath}\" \"{srcPath}\"";
#else
            var command = $"ln -s {srcPath} {destPath}";
#endif
            
            var result = true;
            var code = TryExecuteCmd(command, out _, out var error);
            
            if (code != 0)
            {
                //on osx return code can be not 0
                //double check error
                if (string.IsNullOrEmpty(error))
                {
                    result = true;
                }
                else
                {
                    Debug.LogError($"Failed to link package: {error} src {srcPath} dst {destPath}");
                    result = false;
                }
            }

            info.isLinked = ResourceLinker.IsValidLink(info);
            ResourceLinker.Add(info);
            ReloadLinkedResources();
            
            if (!result) return false;
            
            if (info.isPackage) Client.Resolve();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            return true;
        }
        
        private void AddSymlinkResource(string srcFolderPath)
        {
            if (string.IsNullOrEmpty(srcFolderPath))
            {
                Debug.LogError("No folder selected");
                return;
            }

            srcFolderPath = SymlinkPathTool.FixDirectoryPath(srcFolderPath);

            var link = ResourceLinker.FindResource(srcFolderPath);
            
            if (link is {isLinked: true})
            {
                Debug.LogError("Resource already linked");
                return;
            }
            
            var destFolderPath = SymlinkPathTool.GetDestFolderPath(srcFolderPath);
            destFolderPath = SymlinkPathTool.FixDirectoryPath(destFolderPath);
            link ??= new SymlinkResourceInfo();
            
            link.sourcePath = SymlinkPath.Create(srcFolderPath);
            link.destPath = SymlinkPath.Create(destFolderPath);
            
            CreateSymLink(link);
        }

        public SymlinkResourceInfo UpdatePackageInfo(SymlinkResourceInfo link)
        {
            var sourcePath = link.sourcePath.AbsolutePath;
            var destPath = link.destPath.AbsolutePath;
            
            var packageData = SymlinkPathTool.SelectPackage(sourcePath);
            link.isPackage = packageData.found;
            
            if (!packageData.found)
            {
                link.packageLinkInfo = default;
            }
            else
            {
                link.packageLinkInfo.path = SymlinkPath.Create(destPath);
                link.packageLinkInfo.packageInfo = packageData.info.packageInfo;
            }

            return link;
        }
        
        public void DeleteResourceLink(string path)
        {
            var resource = ResourceLinker.FindResource(path);
            if (resource == null) return;
            DeleteResourceLink(resource);
        }

        public void UnlinkResource(SymlinkResourceInfo link)
        {
            var sourcePath = link.sourcePath;
            var destPath = link.destPath;
            var path = destPath.AbsolutePath;
            var source = sourcePath.AbsolutePath;

            if (Directory.Exists(path))
            {
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
                    }
                }
            }

            SymlinkPathTool.DeleteFolderWithMeta(path);
            
            if (link.isPackage)
            {
                Client.Remove(link.packageLinkInfo.packageInfo.name);
                Client.Resolve();
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            ReloadLinkedResources();
        }

        public void ReloadLinkedResources()
        {
            foreach (var link in ResourceLinker.resources)
            {
                link.isLinked = ResourceLinker.IsValidLink(link);
                UpdatePackageInfo(link);
            }
        }
        
        public void DeleteResourceLink(SymlinkResourceInfo link)
        {
            UnlinkResource(link);

            ResourceLinker.Delete(link);
            ResourceLinker.Save();

            ReloadLinkedResources();
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
            foreach (var resourceInfo in ResourceLinker.resources)
            {   
                if (!resourceInfo.isPackage) continue;
                
                var path = resourceInfo.sourcePath.AbsolutePath;
                var packageJsonPath = SymlinkPathTool.GetPackagePath(path);

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