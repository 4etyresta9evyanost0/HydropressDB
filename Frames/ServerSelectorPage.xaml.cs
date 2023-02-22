using Microsoft.SqlServer.Management.Smo;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.SqlServer.Management.Smo.Wmi;

namespace HydropressDB
{
    /// <summary>
    /// Логика взаимодействия для ServerSelectorPage.xaml
    /// </summary>
    public partial class ServerSelectorPage : Page
    {
        
        public ServerSelectorPage()
        {
            InitializeComponent();
            dgv.SelectionChanged += async (x, ev) =>
            {
                var Mvm = (MainViewModel)Application.Current.Resources["Mvm"];
                lbDb.Items.Clear();
                if (dgv.SelectedIndex != -1)
                {
                    string serverName = ((DataRowView)dgv.SelectedValue)[0].ToString();
                    Server server = null;
                    try
                    {
                        Mvm.LoadingTasks.Add("ServerSelectorPage.Databases.Update");
                        await Task.Run(() => server = new Server(serverName));
                        DatabaseCollection databases = null;
                        await Task.Run(() => databases = server.Databases);
                        Mvm.LoadingTasks.Remove("ServerSelectorPage.Databases.Update");
                        foreach (Database database in databases)
                        {
                            lbDb.Items.Add(database.Name);
                        }
                    }
                    catch
                    {
                        Mvm.LoadingTasks.Remove("ServerSelectorPage.Databases.Update");
                    }
                }
            };
        }

        public ListBox ListBoxDataBases
        {
            get => lbDb;
        }
    }
}
