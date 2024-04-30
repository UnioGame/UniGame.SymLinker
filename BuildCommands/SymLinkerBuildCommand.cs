using UniGame.Symlinks.Symlinker.Editor;
using UniGame.UniBuild.Editor.ClientBuild.Interfaces;
using UniModules.UniGame.UniBuild.Editor.ClientBuild.Commands.PreBuildCommands;

namespace Game.Modules.UniGame.SymLinker.BuildCommands
{
    using System;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class SymLinkerBuildCommand : SerializableBuildCommand
    {
#if ODIN_INSPECTOR
        [FolderPath]
#endif
        public string[] linkResources = Array.Empty<string>();
        
#if ODIN_INSPECTOR
        [FolderPath]
#endif
        public string[] unlinkResources =Array.Empty<string>();
        
        public override void Execute(IUniBuilderConfiguration buildParameters)
        {
            Execute();
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public void Execute()
        {
            var symLinker = new ResourceSymLinker();
            
            foreach (var linkResource in linkResources)
            {
                var symLink = symLinker.Find(linkResource);
                symLink ??= symLinker.CreateLink(linkResource);
                symLinker.RestoreSymLink(symLink);
            }

            foreach (var unlinkResource in unlinkResources)
            {
                var symLink = symLinker.Find(unlinkResource);
                symLink ??= symLinker.CreateLink(unlinkResource);
                symLinker.UnlinkResource(symLink);
            }
        }
    }
}