using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HydropressDB
{
    internal class AuthorizationViewModel : ViewModel
    {
        MainViewModel Mvm { get => (MainViewModel)Application.Current.Resources["Mvm"]; }
        public AuthorizationViewModel()
        {

        }
    }
}
