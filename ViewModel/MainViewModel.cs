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
using Microsoft.Data.SqlClient;
//using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime;
using HydropressDB.HydropressDBDataSetTableAdapters;

namespace HydropressDB
{
    public enum ServerListStatus
    {
        Updating = 1,
        Updated = 0,
        Failed = -1
    }

    public enum UserType
    {
        Guest = -1,
        Admin = 0,
        Constructor = 1,
        Common = 2
    }

    internal class UserSettings : ViewModel
    {
        private string servername;
        private string maindbname;
        private string userdbname;

        private bool autologin;
        private string username;
        private string password;

        public string ServerName
        {
            get => servername;
            set
            {
                servername = value;
                OnPropertyChanged(nameof(ServerName));
            }
        }
        public string MainDbName
        {
            get => maindbname;
            set
            {
                maindbname = value;
                OnPropertyChanged(nameof(MainDbName));
            }
        }
        public string UserDbName
        {
            get => userdbname;
            set
            {
                userdbname = value;
                OnPropertyChanged(nameof(UserDbName));
            }
        }

        public bool Autologin
        {
            get => autologin;
            set
            {
                autologin = value;
                OnPropertyChanged(nameof(Autologin));
            }
        }
        public string Username
        {
            get => username;
            set
            {
                username = value;
                OnPropertyChanged(nameof(Username));
            }
        }
        public string Password
        {
            get => password;
            set
            {
                password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
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
        ObservableCollection<Page> pages = new ObservableCollection<Page>();
        ObservableCollection<Page> sidebarButtons = new ObservableCollection<Page>();
        ObservableCollection<Page> availablePages = new ObservableCollection<Page>();
        ObservableCollection<object> loadingTasks = new ObservableCollection<object>();
        public ObservableCollection<Page> AvailablePages { get => availablePages; }
        DbManagement dbManagement = new DbManagement();
        Page currentPage;
        Page mainPage = new MainMenu();
        Page authPage = new AuthorizationPage();
        Page serverPage = new ServerSelectorPage();
        ServerListStatus serverListStatus = ServerListStatus.Updating;
        //ObservableCollection<(Server, Database[])> servers;
        bool isLoading;
        DataTable availableServers;

        // for further use
        //Server server;
        //Database mainDb;
        //Database userDb;

        UserSettings userSettings = new UserSettings();
        //UserSettings newSettings = new UserSettings();
        bool isInitialized;
        public bool IsInitialized
        {
            get => isInitialized;
            set
            {
                isInitialized = value;
                OnPropertyChanged(nameof(IsInitialized));
            }
        }
        UserType userType;

        // Json
        //public const JsonDocument settingsJson;

        // connections
        SqlConnection mainConnection;
        SqlConnection userConnection;

        //statusbar strings
        //string serverSb;
        //string mainDbSb;
        //string userDbSb;
        //string userSb;

        // time to connect to server
        int connectTimeout = 15;

        // selected db in server page
        private string selectedDb;

        // server page selected items
        private DataRowView selectedServer;

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
            ReadJSONSettingsFile();
            CurrentPage = serverPage;
            LoadingTasks.CollectionChanged += (s, e) => IsLoading = !loadingTasks.IsNullOrEmpty();
            pages.CollectionChanged += (s, e) => { };


            //
            //  Как включить:
            //  Запустить SQL Server Browser service (Служба обозревателя SQL Server)
            //  Это делается из SQL Server Manager (Диспетчер конфигурации SQL Server)
            //  Если не получается, то приложение ищет по реестрам только локальные сервера, иначе серверов нет.
            //
            Task.Run(()=>UpdateServers());

        }

        public async Task CreateJSONSettingsFile()
        {
            using (StreamWriter fs = new StreamWriter(new FileStream("settings.json", FileMode.Create)))
                await fs.WriteAsync(JsonConvert.SerializeObject(UserSettings, Formatting.Indented));
        }

        public void ReadJSONSettingsFile()
        {
            using (StreamReader fs = new StreamReader(new FileStream("settings.json", FileMode.OpenOrCreate)))
            {
                try
                {
                    UserSettings = JsonConvert.DeserializeObject<UserSettings>(fs.ReadToEnd());
                    if (UserSettings == null)
                        UserSettings = new UserSettings();
                }
                catch (Exception e) {
                    MessageBox.Show(e.Message, "Ошибка в чтении файла настроек", MessageBoxButton.OK, MessageBoxImage.Error);
                    UserSettings = new UserSettings();
                }
                //TODO
            }
        }


        // statusbar strings
        //public string ServerName
        //{
        //    get => UserSettings.ServerName ?? "Server is not selected";
        //}
        //public string MainDatabaseName
        //{
        //    get => UserSettings.MainDbName ?? "Main DB is not selected";
        //}
        //public string UserDatabaseName
        //{
        //    get => UserSettings.UserDbName ?? "User DB is not selected";
        //}
        //public string UserName
        //{
        //    get => UserSettings.Username ?? "User is not authorized";
        //}

        public UserType UserType
        {
            get => userType;
            set
            {
                userType = value;
                OnPropertyChanged(nameof(UserType));
            }
        }

        public async Task Authorize(string username, string password)
        {
            var command = $@"SELECT * FROM [Users] WHERE Nickname = '{username}';";
            SqlDataAdapter adapter = new SqlDataAdapter(command, UserConnection);
            // Создаем объект Dataset
            DataSet ds = new DataSet();
            // Заполняем Dataset
            adapter.Fill(ds);
            var row = ds.Tables[0].Rows[0];
            if ((string)row[1] != username)
            {
                MessageBox.Show("Такого пользователя не существует!","Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if ((string)row[2] != password)
            {
                MessageBox.Show("Неправильный пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //REDO
            CurrentPage = mainPage;
            //TableAdapterManager tableAdapterManager = new TableAdapterManager();
            //TODO-REDO

            await CreateJSONSettingsFile();
        }
        // server page commands
        public RelayCommand ConnectToServerCommand
        {
            get
            {
                return connectToServerCommand ??
                    (connectToServerCommand = new RelayCommand(async obj =>
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

                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                        {
                            Pooling = true,
                            IntegratedSecurity = true,
                            TrustServerCertificate = true,
                            ConnectTimeout = 15,
                            DataSource = serverAdressTb.Text,
                            InitialCatalog = mainDbTb.Text,
                        };

                        MainConnection = new SqlConnection(builder.ConnectionString);
                        builder.InitialCatalog = userDbTb.Text;
                        UserConnection = new SqlConnection(builder.ConnectionString);

                        var res = Task.Run(()=>ConnectToServer());
                        LoadingTasks.Add(res);
                        await res;
                        LoadingTasks.Remove(res);

                        if (res.Result == null)
                        {
                            UserSettings.ServerName = builder.DataSource;
                            UserSettings.MainDbName = mainDbTb.Text;
                            UserSettings.UserDbName = userDbTb.Text;

                            CurrentPage = authPage;

                            //UserSettings = new UserSettings
                            //{
                            //    Autologin = false,
                            //    MainDbName = UserSettings.MainDbName,
                            //    UserDbName = UserSettings.UserDbName,
                            //    ServerName = ServerName,
                            //    Username = UserName,
                            //    Password = null,
                            //};

                            await CreateJSONSettingsFile();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось подключится к серверу.\r\nПроверьте правильность введённых данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            UserSettings.ServerName = UserSettings.MainDbName = UserSettings.UserDbName = null;
                        }
                    }));
            }
        }
        public UserSettings UserSettings
        {
            get => userSettings;
            set
            {
                userSettings = value;
                OnPropertyChanged(nameof(UserSettings));
            }
        }
        public async Task<Exception> ConnectToServer()
        {
            try
            {
                var mainTask = MainConnection.OpenAsync();
                var userTask = UserConnection.OpenAsync();

                await mainTask;
                await userTask;

                if (mainTask.Exception != null && userTask != null) 
                    return new AggregateException(mainTask.Exception, userTask.Exception);
                if (mainTask.Exception != null)
                    return mainTask.Exception;
                if (userTask.Exception != null)
                    return userTask.Exception;

                return null;
            }
            catch (Exception ex) { return ex; }
        }
        public async Task<Exception> DisconectFromServer()
        {
            if (MainConnection == null && UserConnection == null)
                return new Exception("MainConnection == null || UserConnection == null");
            try
            {
                var mainTask = Task.Run(() => MainConnection.Close());
                var userTask = Task.Run(() => UserConnection.Close());

                await mainTask;
                await userTask;

                if (mainTask.Exception != null && userTask != null)
                    return new AggregateException(mainTask.Exception, userTask.Exception);
                if (mainTask.Exception != null)
                    return mainTask.Exception;
                if (userTask.Exception != null)
                    return userTask.Exception;
                return null;
            }
            catch (Exception ex) { return ex; }
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
                        if (SelectedServer != null && SelectedServer[0] != null)
                            ((TextBox)obj).Text = SelectedServer == null ? "" : (string)SelectedServer[0];
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
        
        // connections
        public int ConnectionTimeout
        {
            get => connectTimeout;
            set
            {
                connectTimeout = value;
                OnPropertyChanged(nameof(ConnectionTimeout));
            }
        }
        public SqlConnection MainConnection
        {
            get => mainConnection;
            set
            {
                mainConnection = value;
                OnPropertyChanged(nameof(MainConnection));
            }
        }
        public SqlConnection UserConnection
        {
            get => userConnection;
            set
            {
                userConnection = value;
                OnPropertyChanged(nameof(UserConnection));
            }
        }
        public DataRowView SelectedServer
        {
            get => selectedServer;
            set
            {
                selectedServer = value;
                OnPropertyChanged(nameof(SelectedServer));
            }
        }
        
        public string SelectedDb
        {
            get => selectedDb;
            set
            {
                selectedDb = value;
                OnPropertyChanged(nameof(SelectedDb));
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
        public Page MainPage
        {
            get => mainPage;
            set
            {
                mainPage = value;
                OnPropertyChanged(nameof(MainPage));
            }
        }
        public Page AuthPage
        {
            get => authPage;
            set
            {
                authPage = value;
                OnPropertyChanged(nameof(AuthPage));
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
