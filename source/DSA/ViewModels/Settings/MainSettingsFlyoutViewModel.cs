using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Salesforce.SDK.Auth;
using DSA.Model;


namespace DSA.ViewModel.Settings
{
    public class MainSettingsFlyoutViewModel : ViewModelBase
    {
        private SfdcConfig Config
        {
            get { return (SfdcConfig)SDKManager.ServerConfiguration; }
        }

        public bool UseAutoSync
        {
            get { return Config.UseAutoAsync; }
            set
            {
                Config.UseAutoAsync = value;
                RaisePropertyChanged(nameof(UseAutoSync));
            }
        }

        public MainSettingsFlyoutViewModel()
        {

        }

        public override void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.RaisePropertyChanged(propertyName);
            Config.SaveConfig();
        }
    }
}
