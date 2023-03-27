using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;
using XperienceCommunity.TreeNodeSelectorFormControl.Core;
using XperienceCommunity.TreeNodeSelectorFormControl.Installers;
using XperienceCommunity.TreeNodeSelectorFormControl.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: RegisterModule(typeof(TreeNodeSelectorInstallerModule))]


namespace XperienceCommunity.TreeNodeSelectorFormControl.Modules
{
    public class TreeNodeSelectorInstallerModule : Module
    {
        public TreeNodeSelectorInstallerModule() : base(nameof(TreeNodeSelectorInstallerModule))
        {
        }
        protected override void OnPreInit()
        {
            base.OnPreInit();
            Service.Use<ITreeNodeSelectorInstaller, TreeNodeSelectorInstaller>();
        }

        /// <summary>
        /// Initialize the module by creating the ITreeNodeSelectorInstaller
        /// </summary>
        /// <remarks>
        /// The first dependency is created using Service.Resolve, which uses the DI container.
        /// However, all other dependencies in the chain will be created automatically using
        /// constructor-based injection.
        /// </remarks>
        protected override void OnInit()
        {
            if (IsRunningInCmsApp())
            {
                var treeNodeSelectorInstaller = Service.Resolve<ITreeNodeSelectorInstaller>();
                treeNodeSelectorInstaller.Install();
            }
            base.OnInit();
        }

        private static bool IsRunningInCmsApp()
        {
            return (SystemContext.IsCMSRunningAsMainApplication && SystemContext.IsWebSite);
        }

    }
}
