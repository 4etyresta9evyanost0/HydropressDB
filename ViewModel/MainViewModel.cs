using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Wmi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Windows.Data;
using Microsoft.IdentityModel.Tokens;
using HydropressDB.Properties;
using System.Windows.Input;
using System.Data.Common;
using System.Windows;
using Xceed.Wpf.AvalonDock.Controls;
using System.Data.SqlClient;
using System.Text.Json;

namespace HydropressDB
{
    public enum ServerListStatus
    {
        Updating = 1,
        Updated = 0,
        Failed = -1
    }

    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }

    internal class MainViewModel : ViewModel
    {
        ObservableCollection<Page> pages;
        Page currentPage;
        Page authPage = new AuthorizationPage();
        Page serverPage = new ServerSelectorPage();
        ServerListStatus serverListStatus = ServerListStatus.Updating;
        //ObservableCollection<(Server, Database[])> servers;
        bool isLoading;
        ObservableCollection<object> loadingTasks = new ObservableCollection<object>();
        DataTable availableServers;

        // for further use
        Server server;
        Database mainDb;
        Database userDb;

        // Json
        //public const JsonDocument settingsJson;

        // connection strings
        string mainDbConnectionString;
        string userDbConnectionString;

        // connections
        SqlConnection mainConnection;
        SqlConnection userConnection;

        // statusbar 
        string serverName;
        string mainDatabaseName;
        string userDatabaseName;
        string userName;


        // server page selected items
        private DataRowView selectedServer;

        public DataRowView SelectedServer
        {
            get => selectedServer;
            set 
            { 
                selectedServer = value;
                OnPropertyChanged(nameof(SelectedServer));
            }
        }
        private string selectedDb;

        public string SelectedDb
        {
            get => selectedDb;
            set 
            { 
                selectedDb = value; 
                OnPropertyChanged(nameof(SelectedDb));
            }
        }


        // server page commands
        RelayCommand connectToServerCommand;
        RelayCommand updateServesCommand;
        RelayCommand setTextBoxToListBoxCommand;
        RelayCommand setTextBoxToDataGridCommand;

        // window commands
        RelayCommand closeWindowCommand;
        RelayCommand maximizeWindowCommand;
        RelayCommand minimizeWindowCommand;

        public MainViewModel()
        {
            CurrentPage = serverPage;
            LoadingTasks.CollectionChanged += (s, e) => IsLoading = !loadingTasks.IsNullOrEmpty();
            //
            //  Как включить:
            //  Запустить SQL Server Browser service (Служба обозревателя SQL Server)
            //  Это делается из SQL Server Manager (Диспетчер конфигурации SQL Server)
            //  Если не получается, то приложение ищет по реестрам только локальные сервера, иначе серверов нет.
            //
            Task.Run(()=>UpdateServers());
        }

        // statusbar 
        public string ServerName 
        { 
            get => serverName ?? "Server is not selected";
            set
            {
                serverName = value;
                OnPropertyChanged("ServerName");
            }
        }
        public string MainDatabaseName 
        { 
            get => mainDatabaseName ?? "Main DB is not selected";
            set
            {
                mainDatabaseName = value;
                OnPropertyChanged("MainDatabaseName");
            }
        }
        public string UserDatabaseName 
        { 
            get => userDatabaseName ?? "User DB is not selected";
            set
            {
                userDatabaseName = value;
                OnPropertyChanged("UserDatabaseName");
            }
        }
        public string UserName
        {
            get => userName ?? "User is not authorized";
            set
            {
                userName = value;
                OnPropertyChanged("UserName");
            }
        }

        // server page commands
        public RelayCommand ConnectToServerCommand
        {
            get
            {
                return connectToServerCommand ??
                    (connectToServerCommand = new RelayCommand(obj =>
                    {
                        TextBox serverAdressTb = (TextBox)serverPage.FindName("serverAdressTb");
                        TextBox mainDbTb = (TextBox)serverPage.FindName("mainDbTb");
                        TextBox userDbTb = (TextBox)serverPage.FindName("userDbTb");
                        if (serverAdressTb.Text == "")
                        {
                            MessageBox.Show("Необходимо ввести сервер!","Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                        if (mainDbTb.Text == "")
                        {
                            MessageBox.Show("Необходимо ввести название основной базы данных!", "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                        if (userDbTb.Text == "")
                        {
                            MessageBox.Show("Необходимо ввести название базы данных пользователей!", "Внимание", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                    }));
            }
        }
        public RelayCommand UpdateServesCommand
        {
            get
                {
                return updateServesCommand ??
                    (updateServesCommand = new RelayCommand(obj =>
                    {
                        Task.Run(() => UpdateServers());
                    }));
                }
        }
        public RelayCommand SetTextBoxToListBoxCommand
        {
            get
                {
                return setTextBoxToListBoxCommand ??
                    (setTextBoxToListBoxCommand = new RelayCommand(obj =>
                    {
                        ((TextBox)obj).Text = SelectedDb;
                    }));
                }
        }

        public RelayCommand SetTextBoxToDataGridCommand
        {
            get
            {
                return setTextBoxToDataGridCommand ??
                    (setTextBoxToDataGridCommand = new RelayCommand(obj =>
                    {
                        ((TextBox)obj).Text = (string)SelectedServer[0];
                    }));
            }
        }

        // window commands
        public RelayCommand CloseWindowCommand
        {
            get
            {
                return closeWindowCommand ??
                    (closeWindowCommand = new RelayCommand(obj =>Application.Current.Shutdown()));
            }
        }
        public RelayCommand MaximizeWindowCommand
        {
            get
            {
                return maximizeWindowCommand ??
                    (maximizeWindowCommand = new RelayCommand(obj => {
                        if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
                        {
                            Application.Current.MainWindow.WindowState = WindowState.Normal;
                        } 
                        else
                        {
                            Application.Current.MainWindow.WindowState = WindowState.Maximized;
                        }
                    }));
            }
        }
        public RelayCommand MinimizeWindowCommand
        {
            get
            {
                return minimizeWindowCommand ??
                    (minimizeWindowCommand = new RelayCommand(obj => Application.Current.MainWindow.WindowState =
                    Application.Current.MainWindow.WindowState = WindowState.Minimized));
            }
        }
        public bool IsLoading
        {
            get => isLoading;
            private set
            {
                isLoading = value;
                OnPropertyChanged("IsLoading");
            }
        }
        public ServerListStatus ServerListStatus
        {
            get => serverListStatus;
            private set
            {
                serverListStatus = value;
                OnPropertyChanged("ServerListStatus");
            }
        }
        public Page ServerPage
        {
            get => serverPage;
            set
            {
                serverPage = value;
                OnPropertyChanged("ServerPage");
            }
        }
        public Page CurrentPage 
        { 
            get => currentPage;
            set
            {
                currentPage = value;
                OnPropertyChanged("CurrentPage");
            }
        }
        public DataTable AvailableServers { 
            get => availableServers; 
            private set
            {
                availableServers = value;
                //if (availableServers == null)
                //{
                //    ServerListStatus = ServerListStatus.Updating;
                //}
                //if (availableServers != null && availableServers.Rows.Count < 0)
                //{
                //    ServerListStatus = ServerListStatus.Updated;
                //}
                OnPropertyChanged("AvailableServers");
            }
        }
        public ObservableCollection<object> LoadingTasks { get => loadingTasks; }
        public ObservableCollection<Page> Pages { get => pages; }

        public async Task UpdateServers()
        {
            LoadingTasks.Add("ServerList.Update");

            #region GetThroughRegedit
            // Дальнейший код является запасным вариантом нахождения локального сервера

            DataTable dt = new DataTable();
            dt.Columns.Add("Name");
            dt.Columns.Add("Server");
            dt.Columns.Add("Instance");
            dt.Columns.Add("IsClustered").DataType = typeof(bool);
            dt.Columns.Add("Version");
            dt.Columns.Add("IsLocal").DataType = typeof(bool);
            var objRows = GetLocalSqlServerInstances2();
            for (int i = 0; i < objRows.Length; i++)
            {
                var row = dt.NewRow();
                row["Name"] = objRows[i][0];
                row["Server"] = objRows[i][1];
                row["Instance"] = objRows[i][2];
                row["IsClustered"] = objRows[i][3];
                row["Version"] = objRows[i][4];
                row["IsLocal"] = objRows[i][5];
                dt.Rows.Add(row);
            }

            if (dt.Rows.Count > 0)
            {
                AvailableServers = dt;
                ServerListStatus = ServerListStatus.Updated;
                LoadingTasks.Remove("ServerList.Update");
                return;
            }

            serverListStatus = ServerListStatus.Failed;
            #endregion
            #region GetNormally
            // Получить сервера; получить не только локальные сервера

            serverListStatus = ServerListStatus.Updating;
            var t = GetLocalSqlServerInstances();
            await t;

            if (t.Result.Rows.Count != 0)
            {
                AvailableServers = t.Result;
                ServerListStatus = ServerListStatus.Updated;
                LoadingTasks.Remove("ServerList.Update");
                return;
            }
            #endregion

            ServerListStatus = ServerListStatus.Failed;
            LoadingTasks.Remove("ServerList.Update");
        }
        async Task<DataTable> GetLocalSqlServerInstances()
        {
            var mainTask = Task.Run(()=>SmoApplication.EnumAvailableSqlServers());
            await mainTask;
            if (mainTask.IsCompleted && mainTask.Result.Rows.Count != 0)
            {
                return mainTask.Result;
            }
            return null;
        }
        private object[][] GetLocalSqlServerInstances2()
        {
            List<object[]> strings= new List<object[]>();
            string ServerName = Environment.MachineName;
            RegistryView registryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
            {
                RegistryKey instanceKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", false);
                if (instanceKey != null)
                {
                    var valueNames = instanceKey.GetValueNames();
                    for (int i = 0; i < valueNames.Length; i++)
                    {
                        var value = instanceKey.GetValue(valueNames[i]) as string;
                        RegistryKey serverPropsKey = hklm.OpenSubKey($@"SOFTWARE\Microsoft\Microsoft SQL Server\{value}", false);
                        strings.Add(
                            new object[]
                            {
                                ServerName + (valueNames[i] == "MSSQLSERVER" ? "" : "\\" + valueNames[i]),              // Name
                                ServerName,                                                                             // Server
                                (valueNames[i] == "MSSQLSERVER" ? "" : valueNames[i]),                                  // Instance
                                Convert.ToBoolean(                                                                      // | | |
                                    serverPropsKey.OpenSubKey("ClusterState").GetValue("MPT_AGENT_CORE_CNI")) ||        // | | |
                                Convert.ToBoolean(                                                                      // ↓ ↓ ↓
                                    serverPropsKey.OpenSubKey("ClusterState").GetValue("SQL_Engine_Core_Inst")),        // IsClustered
                                serverPropsKey.OpenSubKey("MSSQLServer\\CurrentVersion").GetValue("CurrentVersion"),    // Version
                                true,                                                                                   // IsLocal
                            }
                        );
                    }
                }
            }
            return strings.ToArray();
        }
    }
}
