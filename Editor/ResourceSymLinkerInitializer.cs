using UnityEditor;

namespace UniGame.Symlinks.Editor
{
    public static class ResourceSymLinkerInitializer
    {
        private static ResourceSymLinker _resourceSymLinker;
        
        public static ResourceSymLinker ResourceSymLinker
        {
            get
            {
                return _resourceSymLinker??= new ResourceSymLinker();
            }
        }
        
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            var asset = SymLinkerAsset.instance;
            if (!asset.EnableAutoLink) return;

            RestoreSymLinks();
        }

        [MenuItem("UniGame/ResourceSymlinker/Reload Linked Resources")]
        public static void RestoreSymLinks()
        {
            ResourceSymLinker.RestoreSymLinks();
        }
    }
}