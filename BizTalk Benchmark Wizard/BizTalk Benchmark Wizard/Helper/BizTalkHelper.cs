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
    internal class BizTalkHelper
    {
        #region Constants
        const string CONNECTIONSTRINGFORMAT = "Integrated Security=SSPI;database={0};server={1}";
        const string BIZTALKSCOPE = "root\\MicrosoftBizTalkServer";
        const string BIZTALKAPPLICATIONNAME = "BizTalk Benchmark Wizard";
        const string RECEIVEHOST = "BBW_RxHost";
        const string TRANSMITHOST = "BBW_TxHost";
        const string PROCESSINGHOST = "BBW_PxHost";
        #endregion
        #region Constructor 
        public BizTalkHelper(string server, string database)
        {
            this._server = server;
            this._database = database;
        }
        #endregion
        #region public Methods
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
            
            servers.Add(new Server() { Name = bizTalkDBs.BamDBServerName_ComputerName, Type = ServerType.SQL });

            if(servers.Count(s=>s.Name==bizTalkDBs.RuleEngineDBServerName_ComputerName)==0)
                servers.Add(new Server() { Name = bizTalkDBs.RuleEngineDBServerName_ComputerName, Type = ServerType.SQL });

            if (servers.Count(s => s.Name == bizTalkDBs.SubscriptionDBServerName_ComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.SubscriptionDBServerName_ComputerName, Type = ServerType.SQL });

            if (servers.Count(s => s.Name == bizTalkDBs.TrackingDBServerName_ComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.TrackingDBServerName_ComputerName, Type = ServerType.SQL });

            foreach(string btsServer in GetApplicationServerNames())
            {
                if (servers.Count(s => s.Name == btsServer && s.Type==ServerType.BIZTALK) == 0)
                    servers.Add(new Server() { Name = btsServer, Type = ServerType.BIZTALK });
            }

            return servers;
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
                    _explorer.ConnectionString = string.Format(ConfigurationManager.ConnectionStrings["BizTalkMgmtDatabase"].ConnectionString, this._server, this._database);
                    string s = _explorer.Applications[BIZTALKAPPLICATIONNAME].Name;
                }
                catch { return false; }
                return true;
            }
        }
        /// <summary>
        /// Checks if all Hosts, instances and handlers are installed
        /// </summary>
        public bool IsBizTalkHostsInstalled
        {
            get
            {
                bool RxExist=BizTalkHostsInstalled(RECEIVEHOST);
                bool TxExist=BizTalkHostsInstalled(TRANSMITHOST);
                bool PxExist=BizTalkHostsInstalled(PROCESSINGHOST);
                if (!RxExist || !TxExist || !PxExist)
                    return false;

                return true;
            }
        }
        /// <summary>
        /// Creates all hosts, instances and handlers
        /// </summary>
        /// <param name="servername"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void CreateBizTalkHosts(string servername, string windowsGroup, string username, string password)
        {
            this.StopAllHostInstances();

            string hostName = "BBW_RxHost";
            this.UnInstallAndUnMap(hostName, servername);
            this.CreateHost(hostName, HostType.InProcess, windowsGroup, false, false, false);
            this.CreateHostInstance(hostName, servername, username, password);
            this.CreateHandler(hostName, "WCF-NetTcp", HandlerType.Receive);
            this.StartHostInstance(hostName);

            hostName = "BBW_TxHost";
            this.UnInstallAndUnMap(hostName, servername);
            this.CreateHost(hostName, HostType.InProcess, windowsGroup, false, false, false);
            this.CreateHostInstance(hostName, servername, username, password);
            this.CreateHandler(hostName, "WCF-NetTcp", HandlerType.Send);
            this.StartHostInstance(hostName);

            hostName = "BBW_PxHost";
            this.UnInstallAndUnMap(hostName, servername);
            this.CreateHost(hostName, HostType.InProcess, windowsGroup, false, true, false);
            this.CreateHostInstance(hostName, servername, username, password);
            this.StartHostInstance(hostName);
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
                //Create EnumerationOptions and run wql query
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.ReturnImmediately = false;

                //Search for all HostInstances of 'InProcess' type in the Biztalk namespace scope
                ManagementObjectSearcher searchObject =
                    new ManagementObjectSearcher(BIZTALKSCOPE, "Select * from MSBTS_ServerHost", enumOptions);

                //Enumerate through the result set and start each HostInstance if it is already stopped
                foreach (ManagementObject inst in searchObject.Get())
                {
                    string serverName = inst["servername"] as string;
                    if (!applicationServers.Contains(serverName))
                        applicationServers.Add(serverName);
                }
                return applicationServers;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to find running hosts", ex);
            }
        }
        #endregion

        #region Private Members
        private BtsCatalogExplorer _explorer = new BtsCatalogExplorer();
        private string _server = null;
        private string _database = null;
        private string _btsAdmGroup = null;
        #endregion
        #region Private Methods
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
                            bizTalkDBs.TrackingDBServerName_ComputerName = GetComputerName(bizTalkDBs.TrackingDBServerName);
                        }
                        if (!string.IsNullOrEmpty(reader["SubscriptionDBServerName"].ToString()))
                        {
                            bizTalkDBs.SubscriptionDBServerName = reader["SubscriptionDBServerName"].ToString();
                            bizTalkDBs.SubscriptionDBServerName_ComputerName = GetComputerName(bizTalkDBs.SubscriptionDBServerName);
                        }
                        if (!string.IsNullOrEmpty(reader["BamDBServerName"].ToString()))
                        {
                            bizTalkDBs.BamDBServerName = reader["BamDBServerName"].ToString();
                            bizTalkDBs.BamDBServerName_ComputerName = GetComputerName(bizTalkDBs.BamDBServerName);
                        }
                        if (!string.IsNullOrEmpty(reader["RuleEngineDBServerName"].ToString()))
                        {
                            bizTalkDBs.RuleEngineDBServerName = reader["RuleEngineDBServerName"].ToString();
                            bizTalkDBs.RuleEngineDBServerName_ComputerName = GetComputerName(bizTalkDBs.RuleEngineDBServerName);
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
            using (SqlConnection connection = new SqlConnection(string.Format(CONNECTIONSTRINGFORMAT, "master", server)))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(ConfigurationManager.AppSettings["GetComputerNameQuery"], connection);

                SqlDataReader reader = command.ExecuteReader();
                reader.Read();

                computerName = reader["COMPUTERNAME"].ToString();
                reader.Close();
            }
            return computerName;
        }
        private void CreateCollectorSet(string serverName, string template)
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
        private enum HandlerType { Receive, Send }
        private enum HostType { InProcess = 1, Isolated = 2 }
        private void CreateHost(string hostName, HostType hostType, string ntGroupName, bool authTrusted, bool HostTracking, bool isHost32BitOnly)
        {
            try
            {
                PutOptions options = new PutOptions();
                options.Type = PutType.CreateOnly;

                //create a ManagementClass object and spawn a ManagementObject instance
                ManagementClass objHostSettingClass = new ManagementClass(BIZTALKSCOPE, "MSBTS_HostSetting", null);
                ManagementObject objHostSetting = objHostSettingClass.CreateInstance();

                //set the properties for the Managementobject
                objHostSetting["Name"] = hostName;
                objHostSetting["HostType"] = hostType;
                objHostSetting["NTGroupName"] = ntGroupName;
                objHostSetting["AuthTrusted"] = authTrusted;
                objHostSetting["HostTracking"] = HostTracking;
                objHostSetting["IsHost32BitOnly"] = isHost32BitOnly;

                Type[] targetTypes = new Type[1];
                targetTypes[0] = typeof(PutOptions);

                object[] parameters = new object[1];
                parameters[0] = options;

                Type objType = objHostSetting.GetType();
                MethodInfo mi = objType.GetMethod("Put", targetTypes);
                mi.Invoke(objHostSetting, parameters);

                //create the Managementobject
                //objHostSetting.Put(options);
                System.Console.WriteLine("Host – " + hostName + " – has been created successfully");
            }
            catch (ManagementException mex)
            {
                Console.WriteLine("Management Exception " + mex.Message);
            }
            catch (Exception excep)
            {
                System.Console.WriteLine("CreateHost – " + hostName + " – failed: " + excep.Message);
            }
        }
        private void CreateHostInstance(string hostName, string serverName, string userName, string password)
        {
            try
            {
                //Build the name of the HostInstance - name has to be in the below format
                string hostInstanceName = "Microsoft BizTalk Server" //Name of product
                                  + " " + hostName         //Name of Host of which instance is to be created
                                  + " " + serverName;         //Name of Server on which instance is to be created

                //Create an instance of the ServerHost class using the System.Management namespace
                ObjectGetOptions svrHostOptions = new ObjectGetOptions();
                ManagementClass svrHostClass = new ManagementClass(BIZTALKSCOPE, "MSBTS_ServerHost", svrHostOptions);
                ManagementObject svrHostObject = svrHostClass.CreateInstance();

                //Set the properties of the ServerHost instance
                svrHostObject["ServerName"] = serverName;
                svrHostObject["HostName"] = hostName;

                //Invoke the Map method of the ServerHost instance
                svrHostObject.InvokeMethod("Map", null);

                //Create an instance of the HostInstance class using the System.Management namespace
                ObjectGetOptions hostInstOptions = new ObjectGetOptions();
                ManagementClass hostInstClass = new ManagementClass(BIZTALKSCOPE, "MSBTS_HostInstance", hostInstOptions);
                ManagementObject hostInstObject = hostInstClass.CreateInstance();

                //Set the properties of the HostInstance class
                hostInstObject["Name"] = hostInstanceName;

                //Build a parameter array
                object[] args = new object[2];
                args[0] = userName;
                args[1] = password;

                //Invoke the Install method of the HostInstance
                hostInstObject.InvokeMethod("Install", args);

                Console.WriteLine("HostInstance was mapped and installed successfully. Mapping created between Host: " + hostName + " and Server: " + serverName);
                return;
            }
            catch (Exception excep)
            {
                Console.WriteLine("Failure during HostInstance creation: " + excep.Message);
            }
        }
        private void UnInstallAndUnMap(string hostName, string svrName)
        {
            try
            {
                //Build the HostInstance name
                string hostInstanceName = "Microsoft BizTalk Server" //Name of product
                   + " " + hostName //Name of Host of which instance is to be deleted
                   + " " + svrName; //Name of Server on which instance is to be deleted

                //Get the options and create a new ManagementClass
                ObjectGetOptions hostInstOptions = new ObjectGetOptions();
                ManagementClass hostInstClass = new ManagementClass(BIZTALKSCOPE, "MSBTS_HostInstance", hostInstOptions);
                //Specify the enumeration options and retrieve instances of the HostInstance class
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.ReturnImmediately = false;
                ManagementObjectCollection hostInstCollection = hostInstClass.GetInstances(enumOptions);

                ManagementObject hostInstance = null;

                //Iterate through the collection and retrieve the specific HostInstance that is required
                foreach (ManagementObject inst in hostInstCollection)
                {
                    if (inst["Name"] != null)
                        if (inst["Name"].ToString().ToUpper() == hostInstanceName.ToUpper())
                            hostInstance = inst;
                }

                //Stop the HostInstance if it is 'Started' and if it is an InProcess HostInstance
                if (hostInstance != null && hostInstance["HostType"].ToString() != "2" && hostInstance["ServiceState"].ToString() == "4")
                    hostInstance.InvokeMethod("Stop", null);

                // Remove adapter handlers
                DeleteReceiveHandler("WCF-NetTcp", hostName, HandlerType.Receive);
                DeleteReceiveHandler("WCF-NetTcp", hostName, HandlerType.Send);

                //Now UnInstall the HostInstance
                if (hostInstance != null)
                    hostInstance.InvokeMethod("UnInstall", null);

                //Create an instance of the ServerHost class using the System.Management namespace
                ObjectGetOptions svrHostOptions = new ObjectGetOptions();
                ManagementClass svrHostClass = new ManagementClass(BIZTALKSCOPE, "MSBTS_ServerHost", svrHostOptions);
                ManagementObject svrHostObject = svrHostClass.CreateInstance();

                //Set the properties of the ServerHost instance
                svrHostObject["ServerName"] = svrName;
                svrHostObject["HostName"] = hostName;

                //Invoke the UnMap method of the ServerHost object
                svrHostObject.InvokeMethod("UnMap", null);

                Console.WriteLine("HostInstance was uninstalled and unmapped successfully. Mapping deleted between Host: " + hostName + " and Server: " + svrName);
                return;
            }
            catch (Exception excep)
            {
                Console.WriteLine("Failure during HostInstance deletion - " + excep.Message);
            }
        }
        private void CreateHandler(string hostName, string adapterName, HandlerType handlerType)
        {
            string handler = handlerType == HandlerType.Receive ? "MSBTS_ReceiveHandler" : "MSBTS_SendHandler2";
            try
            {
                PutOptions options = new PutOptions();
                options.Type = PutType.CreateOnly;

                //create a ManagementClass object and spawn a ManagementObject instance
                ManagementClass objReceiveHandlerClass = new ManagementClass(BIZTALKSCOPE, handler, null);
                ManagementObject objReceiveHandler = objReceiveHandlerClass.CreateInstance();

                //set the properties for the Managementobject
                objReceiveHandler["AdapterName"] = adapterName;
                objReceiveHandler["HostName"] = hostName;

                //create the Managementobject
                objReceiveHandler.Put(options);
                System.Console.WriteLine("ReceiveHandler - " + adapterName + " " + hostName + " - has been created successfully");
            }
            catch (Exception excep)
            {
                System.Console.WriteLine("CreateReceiveHandler - " + adapterName + " " + hostName + " - failed: " + excep.Message);
            }
        }
        private void DeleteReceiveHandler(string adapterName, string hostName, HandlerType handlerType)
        {
            string handler = handlerType == HandlerType.Receive ? "MSBTS_ReceiveHandler" : "MSBTS_SendHandler2";
            string query = string.Format("Select * FROM {0} WHERE AdapterName=\"{1}\" AND HostName=\"{2}\"", handler, adapterName, hostName);

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(new ManagementScope(BIZTALKSCOPE), new WqlObjectQuery(query), null);
            ManagementObjectCollection result = searcher.Get();

            IEnumerator enumerator = result.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                Console.WriteLine("Not found");
                return;
            }
            ManagementObject o = (ManagementObject)enumerator.Current;
            o.Delete();
        }
        private void StopAllHostInstances()
        {
            try
            {
                //Create EnumerationOptions and run wql query
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.ReturnImmediately = false;
                //Search for all HostInstances of 'InProcess' type in the Biztalk namespace scope
                ManagementObjectSearcher searchObject = new ManagementObjectSearcher(BIZTALKSCOPE, "Select * from MSBTS_HostInstance where HostType=1", enumOptions);

                //Enumerate through the result set and start each HostInstance if it is already stopped
                foreach (ManagementObject inst in searchObject.Get())
                {
                    //Check if ServiceState is 'Stopped'
                    if (inst["ServiceState"].ToString() == "4")
                    {
                        inst.InvokeMethod("Stop", null);
                    }
                    Console.WriteLine("HostInstance of Host: " + inst["HostName"] + " and Server: " + inst["RunningServer"] + " was started successfully");
                }

                Console.WriteLine("All HostInstances started");
                return;
            }
            catch (Exception excep)
            {
                Console.WriteLine("Failure while starting HostInstances - " + excep.Message);
            }

        }
        private void StartHostInstance(string HostName)
        {
            try
            {
                //Create EnumerationOptions and run wql query
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.ReturnImmediately = false;
                //Search for all HostInstances of 'InProcess' type in the Biztalk namespace scope
                ManagementObjectSearcher searchObject = new ManagementObjectSearcher(BIZTALKSCOPE, "Select * from MSBTS_HostInstance where HostType=1", enumOptions);

                //Enumerate through the result set and start each HostInstance if it is already stopped
                foreach (ManagementObject inst in searchObject.Get())
                {
                    //Check if ServiceState is 'Stopped'
                    if (inst["HostName"].ToString() == HostName && inst["ServiceState"].ToString() == "1")
                    {
                        inst.InvokeMethod("Start", null);
                    }
                    Console.WriteLine("HostInstance of Host: " + inst["HostName"] + " and Server: " + inst["RunningServer"] + " was started successfully");
                }

                Console.WriteLine(HostName + " started");
                return;
            }
            catch (Exception excep)
            {
                Console.WriteLine("Failure while starting HostInstances - " + excep.Message);
            }

        }
        public bool BizTalkHostsInstalled(string hostName)
        {
                string query = string.Format("Select * FROM MSBTS_ServerHost WHERE HostName=\"{0}\"", hostName);

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(new ManagementScope(BIZTALKSCOPE), new WqlObjectQuery(query), null);
                ManagementObjectCollection result = searcher.Get();

                IEnumerator enumerator = result.GetEnumerator();

                bool ret = enumerator.MoveNext();
                return ret;
        }
        #endregion
    }
    internal enum ServerType{BIZTALK, SQL};
    internal class Server
    {
        public string Name{get;set;}
        public ServerType Type {get;set;}
    }
    internal class BizTalkDBs
    {
        public string BizTalkAdminGroup { get; set; }
        public string Default { get; set; }
        public string Default_ComputerName { get; set; }
        public string TrackingDBServerName { get; set; }
        public string TrackingDBServerName_ComputerName { get; set; }
        public string SubscriptionDBServerName { get; set; }
        public string SubscriptionDBServerName_ComputerName { get; set; }
        public string BamDBServerName { get; set; }
        public string BamDBServerName_ComputerName { get; set; }
        public string RuleEngineDBServerName { get; set; }
        public string RuleEngineDBServerName_ComputerName { get; set; }
    }
}
