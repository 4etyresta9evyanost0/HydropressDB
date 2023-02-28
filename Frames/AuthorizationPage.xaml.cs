using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HydropressDB
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationPage.xaml
    /// </summary>
    public partial class AuthorizationPage : Page
    {
        AuthorizationViewModel AuthVm { get => (AuthorizationViewModel)Application.Current.Resources["AuthVm"]; }
        MainViewModel Mvm { get => (MainViewModel)Application.Current.Resources["Mvm"]; }
        public AuthorizationPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Mvm.Authorize(loginTb.Text, passwordTb.Password);
        }
    }
}
