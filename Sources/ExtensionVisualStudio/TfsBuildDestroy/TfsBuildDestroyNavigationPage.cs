using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;


namespace TfsBuildDestroy
{
    [TeamExplorerPage(GuidList.tfsBuildDestroyNavigationPage)]

    public class TfsBuildDestroyNavigationPage : TeamExplorerBasePage
    {
        
        private ITeamFoundationContextManager _contextManager;


        public object PageContent
        {
            get
            {
                if (_contextManager == null)
                {
                    _contextManager =
                        this.GetService<ITeamFoundationContextManager>() as ITeamFoundationContextManager;
                    
                }


                return new MainView(_contextManager.CurrentContext, GetService<ITeamExplorer>());
            }
        }




        public string Title
        {
            get
            {
                return "TFS Build Destroy";
            }
        }

 

        public event PropertyChangedEventHandler PropertyChanged;

        private void FirePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
