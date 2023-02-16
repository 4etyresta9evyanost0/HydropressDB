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

namespace HydropressDB
{
    public enum ServerListStatus
    {
        Updating = 1,
        Updated = 0,
        Failed = -1
    }

    internal class MainViewModel : ViewModel
    {
        Page currentPage;
        Page serverPage = new ServerSelectorPage();
        ServerListStatus serverListStatus = ServerListStatus.Updating;
        DataTable availableServers;

        public MainViewModel()
        {
            currentPage = serverPage;
            //
            //  Как включить:
            //  Запустить SQL Server Browser service (Служба обозревателя SQL Server)
            //  Это делается из SQL Server Manager (Диспетчер конфигурации SQL Server)
            //  Если не получается:

            Task.Run(()=>UpdateServers());
            //UpdateServers();
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
        public ObservableCollection<Page> Pages = new ObservableCollection<Page>();
        public ServerListStatus ServerListStatus
        {
            get => serverListStatus;
            private set
            {
                serverListStatus = value;
                OnPropertyChanged("ServerListStatus");
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

        public async Task UpdateServers()
        {
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
                return;
            }
            #endregion
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
                                ServerName + "\\" + valueNames[i],                                                      // Name
                                ServerName,                                                                             // Server
                                valueNames[i],                                                                          // Instance
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
