using System.Collections.Generic;
using UnityEditor;

namespace UniGame.Symlinks.Symlinker.Editor
{
    [FilePath("UniGame/ResourceSymlinker/SymlinkerAsset.asset", FilePathAttribute.Location.PreferencesFolder)]
    public class SymLinkerAsset : ScriptableSingleton<SymLinkerAsset>
    {
        public string ProjectResourcePath = "ExternalAssets/";
        
        public List<SymlinkResourceInfo> resources = new();
    }
}