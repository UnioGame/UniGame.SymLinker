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
        private List<SymlinkResourceInfo> _links = new();
        
        public SymLinkerAsset ResourceLinker => SymLinkerAsset.instance;

        public IReadOnlyList<SymlinkResourceInfo> All => ResourceLinker.resources;


        public void EnableAll()
        {
            ReloadLinkedResources();
            RefreshPackageResources();
        }

        public void Refresh()
        {
            RefreshPackageResources();
        }

        public bool IsValidLink(SymlinkResourceInfo link)
        {
            var sourcePath = link.sourcePath;
            var destPath = link.destPath;
            return Directory.Exists(sourcePath.AbsolutePath) &&
                   Directory.Exists(destPath.AbsolutePath);
        }

        public void ReloadLinkedResources()
        {
            _links.Clear();

            foreach (var link in ResourceLinker.resources)
            {
                var sourcePath = link.sourcePath;
                var destPath = link.destPath;

                var isValidLink = IsValidLink(link);
                if (!isValidLink) continue;
                
                UpdatePackageInfo(link);
                
                _links.Add(link);
                
                continue;
            }

            ResourceLinker.resources.Clear();
            ResourceLinker.resources.AddRange(_links);
            
            EditorUtility.SetDirty(ResourceLinker);
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
            {
                RestoreSymLink(resource);
            }
        }

        public void RestoreSymLink(SymlinkResourceInfo link)
        {
            if (IsValidLink(link))return;
            
            DeleteResourceLink(link);
            UpdatePackageInfo(link);
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            CreateSymLink(link);
        }

        public bool CreateSymLink(SymlinkResourceInfo info)
        {
            var srcPath = info.sourcePath.AbsolutePath;
            var destPath = info.destPath.AbsolutePath;

            var link = ResourceLinker.FindResource(srcPath);
            if (link != null) return false;
            
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
            
            UpdatePackageInfo(info);

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

            if (!result) return false;
            
            ResourceLinker.resources.Add(info);
            
            if (info.isPackage) Client.Resolve();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            ReloadLinkedResources();
            
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
            
            if (link!=null)
            {
                Debug.LogError("Resource already linked");
                return;
            }
            
            var destFolderPath = SymlinkPathTool.GetDestFolderPath(srcFolderPath);
            destFolderPath = SymlinkPathTool.FixDirectoryPath(destFolderPath);
            link = new SymlinkResourceInfo()
            {
                sourcePath = SymlinkPath.Create(srcFolderPath),
                destPath = SymlinkPath.Create(destFolderPath),
            };

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

        public void DeleteResourceLink(SymlinkResourceInfo into)
        {
            var sourcePath = into.sourcePath;
            var destPath = into.destPath;
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
                
                SymlinkPathTool.DeleteLinkedFolderAsset(path);
            }

            ResourceLinker.Delete(into);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

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