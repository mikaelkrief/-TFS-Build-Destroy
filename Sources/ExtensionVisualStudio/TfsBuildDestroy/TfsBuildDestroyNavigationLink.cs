using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;

namespace TfsBuildDestroy
{
    [TeamExplorerNavigationLink(GuidList.tfsBuildDestroyNavigationItem, TeamExplorerNavigationItemIds.Builds, 200)]
    public class TfsBuildDestroyNavigationLink :  TeamExplorerBaseNavigationLink
    {
        

        [ImportingConstructor]
        public TfsBuildDestroyNavigationLink([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            
            this.IsVisible = true;
            this.IsEnabled = true;
            this.Text = "TFS Build Destroy";
        }

        public override void Execute()
        {
            var service = this.GetService<ITeamExplorer>();
            if (service == null)
            {
                return;
            }
            service.NavigateToPage(new Guid(GuidList.tfsBuildDestroyNavigationPage), null);
        }



        



    }
}
