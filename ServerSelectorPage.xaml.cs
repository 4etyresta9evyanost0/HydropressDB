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
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            dgv.SelectionChanged += (x, ev) =>
            {
                lbDb.Items.Clear();
                if (dgv.SelectedIndex != -1)
                {
                    string serverName = ((DataRowView)dgv.SelectedValue)[0].ToString();
                    Server server = new Server(serverName);
                    
                    try
                    {
                        foreach (Database database in server.Databases)
                        {
                            lbDb.Items.Add(database.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        string exception = ex.Message;
                    }
                }
            };
        }
    }
}
