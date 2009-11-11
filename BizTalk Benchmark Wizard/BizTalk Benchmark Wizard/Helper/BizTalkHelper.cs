using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.BizTalk.ExplorerOM;
using System.Management;
using System.Data.SqlClient;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Configuration;

namespace BizTalk_Benchmark_Wizard.Helper
{
    internal class BizTalkHelper : IDisposable
    {
        #region Delegates and Events
        public delegate void InitiateStepHandler(object sender, StepEventArgs e);
        public event InitiateStepHandler OnStepComplete;
        #endregion
        #region Constants
        const string CONNECTIONSTRINGFORMAT = "Integrated Security=SSPI;database={0};server={1}";
        const string BIZTALKSCOPE = @"\\{0}\root\MicrosoftBizTalkServer";
        const string BIZTALKAPPLICATIONNAME = "BizTalk Benchmark Wizard";
        const string RECEIVEHOST = "BBW_RxHost";
        const string TRANSMITHOST = "BBW_TxHost";
        const string PROCESSINGHOST = "BBW_PxHost";
        const string ORCHESTRATIONNAME = "EmptySchedule_Baseline1.SimpleSchedule";
        #endregion
        #region Constructor 
        public BizTalkHelper(string server, string database)
        {
            this._server = server;
            this._database = database;
        }
        #endregion
        #region Private Members
        private BtsCatalogExplorer _explorer = new BtsCatalogExplorer();
        private string _server;
        private string _database;
        private string _btsAdmGroup;
        private string _mainBizTalkServer = string.Empty; //Used for WMI
        #endregion
        #region Public Members
        public string NewIndigoUri { get; set; }
        #endregion
        #region Private Methods
        void RaiseInitiateStepEvent(string eventStep)
        {
            if (OnStepComplete != null)
            {
                OnStepComplete(null, new StepEventArgs() { EventStep = eventStep });
            }
        }
        private BizTalkDBs GetSqlServerNames()
        {
            BizTalkDBs bizTalkDBs = new BizTalkDBs();
            try
            {
                using (SqlConnection connection = new SqlConnection(string.Format(CONNECTIONSTRINGFORMAT, _database, _server)))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(ConfigurationManager.AppSettings["GetSqlServersQuery"], connection);

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        if (!string.IsNullOrEmpty(reader["TrackingDBServerName"].ToString()))
                        {
                            bizTalkDBs.TrackingDBServerName = reader["TrackingDBServerName"].ToString();
                            bizTalkDBs.TrackingDBServerNameComputerName = GetComputerName(bizTalkDBs.TrackingDBServerName);
                        }
                        if (!string.IsNullOrEmpty(reader["SubscriptionDBServerName"].ToString()))
                        {
                            bizTalkDBs.SubscriptionDBServerName = reader["SubscriptionDBServerName"].ToString();
                            bizTalkDBs.SubscriptionDBServerNameComputerName = GetComputerName(bizTalkDBs.SubscriptionDBServerName);
                        }
                        if (!string.IsNullOrEmpty(reader["BamDBServerName"].ToString()))
                        {
                            bizTalkDBs.BamDBServerName = reader["BamDBServerName"].ToString();
                            bizTalkDBs.BamDBServerNameComputerName = GetComputerName(bizTalkDBs.BamDBServerName);
                        }
                        if (!string.IsNullOrEmpty(reader["RuleEngineDBServerName"].ToString()))
                        {
                            bizTalkDBs.RuleEngineDBServerName = reader["RuleEngineDBServerName"].ToString();
                            bizTalkDBs.RuleEngineDBServerNameComputerName = GetComputerName(bizTalkDBs.RuleEngineDBServerName);
                        }
                        if (!string.IsNullOrEmpty(reader["BizTalkAdminGroup"].ToString()))
                        {
                            bizTalkDBs.BizTalkAdminGroup = reader["BizTalkAdminGroup"].ToString();
                        }

                    }
                    reader.Close();

                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to find SQL Servers", ex);
            }

            return bizTalkDBs;
        }
        private string GetComputerName(string server)
        {
            string computerName = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(string.Format(CONNECTIONSTRINGFORMAT, "master", server)))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(ConfigurationManager.AppSettings["GetComputerNameQuery"], connection);

                    SqlDataReader reader = command.ExecuteReader();
                    reader.Read();

                    computerName = reader["COMPUTERNAME"].ToString();
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to get the name of the physical server (SQL)", ex);
            }
            return computerName;
        }
        private void CreateCollectorSet(string serverName, string template)
        {
            try
            {
                if (string.IsNullOrEmpty(serverName))
                    return;
                string rootPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), string.Format("COUNTERLOGS\\{0}", serverName));
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                string logPath = Path.Combine(rootPath, "000001");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);

                XmlDocument serverColectorDocument = new XmlDocument();
                serverColectorDocument.Load(template);

                serverColectorDocument.SelectSingleNode("DataCollectorSet/Name").InnerText = serverName;
                serverColectorDocument.SelectSingleNode("DataCollectorSet/Description").InnerText = "Created by BizTalk benchmark wizard";
                serverColectorDocument.SelectSingleNode("DataCollectorSet/OutputLocation").InnerText = logPath;
                serverColectorDocument.SelectSingleNode("DataCollectorSet/RootPath").InnerText = rootPath;


                serverColectorDocument.SelectSingleNode("DataCollectorSet/PerformanceCounterDataCollector/Name").InnerText = serverName;
                serverColectorDocument.SelectSingleNode("DataCollectorSet/PerformanceCounterDataCollector/FileName").InnerText = serverName;

                foreach (XmlNode counterNode in serverColectorDocument.SelectNodes("DataCollectorSet/PerformanceCounterDataCollector/Counter"))
                    counterNode.InnerText = counterNode.InnerText.Replace("@SERVERNAME", serverName);

                foreach (XmlNode counterNode in serverColectorDocument.SelectNodes("DataCollectorSet/PerformanceCounterDataCollector/CounterDisplayName"))
                    counterNode.InnerText = counterNode.InnerText.Replace("@SERVERNAME", serverName);


                string filename = serverName + "_" + Path.GetFileName(template);
                if (!Directory.Exists(Path.Combine(rootPath, "Templates")))
                    Directory.CreateDirectory(Path.Combine(rootPath, "Templates"));

                string fileSaveName = Path.Combine(Path.Combine(rootPath, "Templates"), filename);
                serverColectorDocument.Save(fileSaveName);

                using (ProcessHelper p = new ProcessHelper())
                {
                    string format = string.Format(@"import -name ""{0}"" -xml ""{1}""", filename.Replace(".xml", ""), fileSaveName);
                    p.Execute("logman", format, 30000);
                }
                if (!string.IsNullOrEmpty(ProcessHelper.ErrorMessage) || !string.IsNullOrEmpty(ProcessHelper.OutPutMessage))
                    throw new ApplicationException("Unable to create Data Collector Set");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to create Data Collector Sets", ex);
            }
        }
        private enum HandlerType { Receive, Send }
        private enum HostType { InProcess = 1, Isolated = 2 }
        #endregion
        #region Public Methods
        /// <summary>
        /// Queries the management database for all servers
        /// </summary>
        /// <param name="server"></param>
        /// <param name="mgmtDatabase"></param>
        /// <returns></returns>
        public List<Server> GetServers(string server, string mgmtDatabase)
        {
            List<Server> servers = new List<Server>();
            BizTalkDBs bizTalkDBs = GetSqlServerNames();
            this._btsAdmGroup = bizTalkDBs.BizTalkAdminGroup;

            servers.Add(new Server() { Name = bizTalkDBs.BamDBServerNameComputerName, Type = ServerType.SQL });

            if (servers.Count(s => s.Name == bizTalkDBs.RuleEngineDBServerNameComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.RuleEngineDBServerNameComputerName, Type = ServerType.SQL });

            if (servers.Count(s => s.Name == bizTalkDBs.SubscriptionDBServerNameComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.SubscriptionDBServerNameComputerName, Type = ServerType.SQL });

            if (servers.Count(s => s.Name == bizTalkDBs.TrackingDBServerNameComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.TrackingDBServerNameComputerName, Type = ServerType.SQL });

            
            foreach (string btsServer in GetApplicationServerNames())
            {
                if (servers.Count(s => s.Name == btsServer && s.Type == ServerType.BIZTALK) == 0)
                    servers.Add(new Server() { Name = btsServer, Type = ServerType.BIZTALK });
            }

            return servers;
        }
        /// <summary>
        /// Returns all Host to Server mappings
        /// </summary>
        /// <returns></returns>
        public List<HostMaping> GetHostMappings()
        {
            List<HostMaping> hostMappings = new List<HostMaping>();
            try
            {
                using (SqlConnection connection = new SqlConnection(string.Format(CONNECTIONSTRINGFORMAT, _database, _server)))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(ConfigurationManager.AppSettings["GetHostMappings"], connection);

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        if (hostMappings.Exists(h => h.HostName == reader["Host"].ToString()))
                            hostMappings.First(h => h.HostName == reader["Host"].ToString()).BizTalkServers.Add(reader["Server"].ToString());
                        else
                        {
                            HostMaping h = new HostMaping() { HostName = reader["Host"].ToString(), SelectedHost = reader["Server"].ToString() };
                            h.BizTalkServers.Add(reader["Server"].ToString());
                            switch (reader["Host"].ToString())
                            { 
                                case "BBW_RxHost":
                                    h.HostDescription = "Receive host (BBW_RxHost)";
                                    break;
                                case "BBW_TxHost":
                                    h.HostDescription = "Send host (BBW_TxHost)";
                                    break;
                                case "BBW_PxHost":
                                    h.HostDescription = "Processing host (BBW_PxHost)";
                                    break;
                            }
                            hostMappings.Add(h);
                        }
                    }
                    connection.Close();
                }
                return hostMappings;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to get Host mappings", ex);
            }
        }
        /// <summary>
        /// Checks if the "BizTalk Benchmark Wizard" application is installed, using ExplorerOM
        /// </summary>
        public bool IsBizTalkScenariosInstalled
        {
            get
            {
                try
                {
                    _explorer.ConnectionString = new SqlConnectionStringBuilder()
                                                    {
                                                        DataSource = this._server, 
                                                        InitialCatalog = this._database, 
                                                        IntegratedSecurity=true
                                                    }.ConnectionString;
                        
                    string s = _explorer.Applications[BIZTALKAPPLICATIONNAME].Name;
                }
                catch { return false; }
                return true;
            }
        }
        /// <summary>
        /// Returns a list of application servers
        /// </summary>
        /// <returns></returns>
        public List<string> GetApplicationServerNames()
        {
            List<string> applicationServers = new List<string>();
            try
            {
                using (SqlConnection connection = new SqlConnection(string.Format(CONNECTIONSTRINGFORMAT, _database, _server)))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand(ConfigurationManager.AppSettings["GetBizTalkServersQuery"], connection);

                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        if (!string.IsNullOrEmpty(reader["Name"].ToString()))
                        {
                            if (!applicationServers.Contains(reader["Name"].ToString()))
                                applicationServers.Add(reader["Name"].ToString());
                        }
                    }
                    reader.Close();
                }

                if (applicationServers.Count == 0)
                    throw new ApplicationException("No BizTalk Servers found");

                _mainBizTalkServer = applicationServers[0];
                return applicationServers;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to find running hosts", ex);
            }
        }
        public bool UpdateSendPortUri(string portName, string sendHost)
        {
            try
            {
                string sendPortAddress = _explorer.SendPorts[portName].PrimaryTransport.Address;
                Uri oldAddress = new Uri(sendPortAddress);
                Uri newAddress = new Uri(string.Format("{0}://{1}:{2}{3}",
                            oldAddress.Scheme,
                            sendHost,
                            oldAddress.Port.ToString(),
                            oldAddress.AbsolutePath));

                this.NewIndigoUri = newAddress.ToString();

                _explorer.SendPorts[portName].PrimaryTransport.Address = newAddress.ToString();
                _explorer.SaveChanges();
                RaiseInitiateStepEvent("UpdateSendPortUri");
            }
            catch (Exception ex)
            {
                return false;
                //                throw new ApplicationException("Unable to update the Uri of the IndigoService send port.\n" +  ex.Message);
            }
            return true;
        }
        public void StartBizTalkPorts()//(string receivePort, string receiveLocation, string sendPort, string orchestration)
        {
            try
            {
                foreach (SendPort sendPort in _explorer.Applications[BIZTALKAPPLICATIONNAME].SendPorts)
                    sendPort.Status = PortStatus.Started;

                foreach (ReceivePort receivePort in _explorer.Applications[BIZTALKAPPLICATIONNAME].ReceivePorts)
                {
                    foreach (ReceiveLocation receiveLocation in receivePort.ReceiveLocations)
                        receiveLocation.Enable = true;
                }
                _explorer.Applications["BizTalk Benchmark Wizard"].Orchestrations[ORCHESTRATIONNAME].Status = OrchestrationStatus.Started;

                _explorer.SaveChanges();
                RaiseInitiateStepEvent("CheckPortStatus");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to enable/start ports and orchestrations", ex);
            }
        }
        public int GetNumberOfMsgBoxes()
        { 
            BizTalkDBs bizTalkDBs = new BizTalkDBs();
            try
            {
                int numberOfMsgBoxes;
                using (SqlConnection connection = new SqlConnection(string.Format(CONNECTIONSTRINGFORMAT, _database, _server)))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(ConfigurationManager.AppSettings["GetMsgBoxServersQuery"], connection);

                    SqlDataReader reader = command.ExecuteReader();
                    reader.Read();
                    numberOfMsgBoxes = string.IsNullOrEmpty(reader["MSGBOXCOUNT"].ToString()) ? 1 : (int)reader["MSGBOXCOUNT"];
                    reader.Close();
                }
                return numberOfMsgBoxes;
            }
            catch
            {
                return 1;
            }
        }
        #endregion
        #region IDisposable Members

        public void Dispose()
        {
            if (this._explorer != null)
                _explorer.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
    public enum ServerType{BIZTALK, SQL};
    public class Server
    {
        public string Name{get;set;}
        public ServerType Type {get;set;}
    }
    public class BizTalkDBs
    {
        public string BizTalkAdminGroup { get; set; }
        public string Default { get; set; }
        public string DefaultComputerName { get; set; }
        public string TrackingDBServerName { get; set; }
        public string TrackingDBServerNameComputerName { get; set; }
        public string SubscriptionDBServerName { get; set; }
        public string SubscriptionDBServerNameComputerName { get; set; }
        public string BamDBServerName { get; set; }
        public string BamDBServerNameComputerName { get; set; }
        public string RuleEngineDBServerName { get; set; }
        public string RuleEngineDBServerNameComputerName { get; set; }
    }
    public class HostMaping
    {
        public string HostName { get; set; }
        public string HostDescription { get; set; }
        public string SelectedHost { get; set; }
        public List<string> BizTalkServers = new List<string>();
        public IEnumerable<string> Servers
        {
            get { return (IEnumerable<string>)BizTalkServers; }
        }
    }

}