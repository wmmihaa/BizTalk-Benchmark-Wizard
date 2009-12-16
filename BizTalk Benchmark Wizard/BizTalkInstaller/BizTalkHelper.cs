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

namespace BizTalkInstaller
{
    internal class BizTalkHelper
    {
        #region Constants
        const string CONNECTIONSTRINGFORMAT = "Integrated Security=SSPI;database={0};server={1}";
        const string BIZTALKSCOPE = @"\\{0}\root\MicrosoftBizTalkServer";
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
        #region Private Members
        private BtsCatalogExplorer _explorer = new BtsCatalogExplorer();
        private string _server = null;
        private string _database = null;
        private string _btsAdmGroup = null;
        private string _mainBizTalkServer = string.Empty; //Used for WMI
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
        private enum HandlerType { Receive, Send }
        private enum HostType { InProcess = 1, Isolated = 2 }
        private void CreateHost(string serverName, string hostName, HostType hostType, string ntGroupName, bool authTrusted, bool HostTracking, bool isHost32BitOnly)
        {
            try
            {
                PutOptions options = new PutOptions();
                options.Type = PutType.CreateOnly;

                //create a ManagementClass object and spawn a ManagementObject instance
                ManagementClass objHostSettingClass = new ManagementClass(string.Format(BIZTALKSCOPE, serverName), "MSBTS_HostSetting", null);
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
                throw new ApplicationException("Management Exception", mex);
            }
            catch (Exception excep)
            {
                throw new ApplicationException("CreateHost – " + hostName + " – failed", excep);
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
                ManagementClass svrHostClass = new ManagementClass(string.Format(BIZTALKSCOPE, serverName), "MSBTS_ServerHost", svrHostOptions);
                ManagementObject svrHostObject = svrHostClass.CreateInstance();

                //Set the properties of the ServerHost instance
                svrHostObject["ServerName"] = serverName;
                svrHostObject["HostName"] = hostName;

                //Invoke the Map method of the ServerHost instance
                svrHostObject.InvokeMethod("Map", null);

                //Create an instance of the HostInstance class using the System.Management namespace
                ObjectGetOptions hostInstOptions = new ObjectGetOptions();
                ManagementClass hostInstClass = new ManagementClass(string.Format(BIZTALKSCOPE, serverName), "MSBTS_HostInstance", hostInstOptions);
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
                throw new ApplicationException("Failure during HostInstance creation", excep);
            }
        }
        private void CreateHandler(string hostName, string serverName, string adapterName, HandlerType handlerType)
        {
            string handler = handlerType == HandlerType.Receive ? "MSBTS_ReceiveHandler" : "MSBTS_SendHandler2";
            try
            {
                PutOptions options = new PutOptions();
                options.Type = PutType.CreateOnly;

                //create a ManagementClass object and spawn a ManagementObject instance
                ManagementClass objReceiveHandlerClass = new ManagementClass(string.Format(BIZTALKSCOPE, serverName), handler, null);
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
                throw new ApplicationException("Failed while creating receive handler", excep);

            }
        }
        private void StartHostInstance(string HostName, string serverName)
        {
            try
            {
                //Create EnumerationOptions and run wql query
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.ReturnImmediately = false;
                //Search for all HostInstances of 'InProcess' type in the Biztalk namespace scope
                ManagementObjectSearcher searchObject = new ManagementObjectSearcher(string.Format(BIZTALKSCOPE, serverName), "Select * from MSBTS_HostInstance where HostType=1", enumOptions);

                //Enumerate through the result set and start each HostInstance if it is already stopped
                foreach (ManagementObject inst in searchObject.Get())
                {
                    //Check if ServiceState is 'Stopped'
                    if (inst["HostName"].ToString() == HostName && inst["ServiceState"].ToString() == "1")
                    {
                        inst.InvokeMethod("Start", null);
                    }
                }

                return;
            }
            catch (Exception excep)
            {
                throw new ApplicationException("Failure while starting HostInstances - ", excep);
            }

        }
        public bool BizTalkHostsInstalled(string hostName, string serverName)
        {
            string query = string.Format("Select * FROM MSBTS_ServerHost WHERE HostName=\"{0}\"", hostName);

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(new ManagementScope(string.Format(BIZTALKSCOPE, serverName)), new WqlObjectQuery(query), null);
            ManagementObjectCollection result = searcher.Get();

            IEnumerator enumerator = result.GetEnumerator();

            bool ret = enumerator.MoveNext();
            return ret;
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

            if (servers.Count(s => s.Name == bizTalkDBs.RuleEngineDBServerName_ComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.RuleEngineDBServerName_ComputerName, Type = ServerType.SQL });

            if (servers.Count(s => s.Name == bizTalkDBs.SubscriptionDBServerName_ComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.SubscriptionDBServerName_ComputerName, Type = ServerType.SQL });

            if (servers.Count(s => s.Name == bizTalkDBs.TrackingDBServerName_ComputerName) == 0)
                servers.Add(new Server() { Name = bizTalkDBs.TrackingDBServerName_ComputerName, Type = ServerType.SQL });


            foreach (string btsServer in GetApplicationServerNames())
            {
                if (servers.Count(s => s.Name == btsServer && s.Type == ServerType.BIZTALK) == 0)
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
                //bool RxExist = BizTalkHostsInstalled(RECEIVEHOST, _mainBizTalkServer);
                //bool TxExist = BizTalkHostsInstalled(TRANSMITHOST, _mainBizTalkServer);
                //bool PxExist = BizTalkHostsInstalled(PROCESSINGHOST, _mainBizTalkServer);
                //if (!RxExist || !TxExist || !PxExist)
                //    return false;

                return true;
            }
        }
        /// <summary>
        /// Creates all hosts, instances and handlers
        /// </summary>
        /// <param name="servername"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void CreateBizTalkHosts(string servername, string windowsGroup, string username, string password, bool stopAll)
        {
            if(stopAll)
                this.StopAllHostInstances();

            string hostName = "BBW_RxHost";
            this.CreateHost(servername, hostName, HostType.InProcess, windowsGroup, false, false, false);
            this.CreateHostInstance(hostName, servername, username, password);
            this.CreateHandler(hostName, servername, "WCF-NetTcp", HandlerType.Receive);

            hostName = "BBW_TxHost";
            this.CreateHost(servername, hostName, HostType.InProcess, windowsGroup, false, false, false);
            this.CreateHostInstance(hostName, servername, username, password);
            this.CreateHandler(hostName, servername, "WCF-NetTcp", HandlerType.Send);

            hostName = "BBW_PxHost";
            this.CreateHost(servername, hostName, HostType.InProcess, windowsGroup, false, true, false);
            this.CreateHostInstance(hostName, servername, username, password);

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

                ////Create EnumerationOptions and run wql query
                //EnumerationOptions enumOptions = new EnumerationOptions();
                //enumOptions.ReturnImmediately = false;

                ////Search for all HostInstances of 'InProcess' type in the Biztalk namespace scope
                //ManagementObjectSearcher searchObject =
                //    new ManagementObjectSearcher(string.Format(BIZTALKSCOPE, serverName), "Select * from MSBTS_ServerHost", enumOptions);

                ////Enumerate through the result set and start each HostInstance if it is already stopped
                //foreach (ManagementObject inst in searchObject.Get())
                //{
                //    string serverName = inst["servername"] as string;
                //    if (!applicationServers.Contains(serverName))
                //        applicationServers.Add(serverName);
                //}
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
        public void StartBizTalkHostsInstance(string hostName, string serverName)
        {
            return; //Can't be done
            try
            {
                //Create EnumerationOptions and run wql query
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.ReturnImmediately = false;
                //Search for all HostInstances of 'InProcess' type in the Biztalk namespace scope
                ManagementObjectSearcher searchObject =
                    new ManagementObjectSearcher(string.Format(BIZTALKSCOPE, serverName),
                        string.Format("Select * from MSBTS_HostInstance where HostType=1 and HostName='{0}' and RunningServer='{1}'", hostName, serverName), enumOptions);

                //Enumerate through the result set and start each HostInstance if it is already stopped
                foreach (ManagementObject inst in searchObject.Get())
                {
                    inst.InvokeMethod("Start", null);
                }

                return;
            }
            catch (Exception excep)
            {
                throw new ApplicationException(string.Format("Failed while start Host{0} at {1}.", hostName, serverName) + excep.Message);
            }
        }
        public void StopAllHostInstances()
        {
            return; //Can't be done
            try
            {
                //Create EnumerationOptions and run wql query
                EnumerationOptions enumOptions = new EnumerationOptions();
                enumOptions.ReturnImmediately = false;
                //Search for all HostInstances of 'InProcess' type in the Biztalk namespace scope
                ManagementObjectSearcher searchObject = new ManagementObjectSearcher(string.Format(BIZTALKSCOPE, _mainBizTalkServer), "Select * from MSBTS_HostInstance where HostType=1", enumOptions);

                //Enumerate through the result set and start each HostInstance if it is already stopped
                foreach (ManagementObject inst in searchObject.Get())
                {
                    //Check if ServiceState is 'Stopped'
                    if (inst["ServiceState"].ToString() == "4")
                    {
                        inst.InvokeMethod("Stop", null);
                    }
                }

                return;
            }
            catch (Exception excep)
            {
                throw new ApplicationException("Failure while stopping HostInstances - ", excep);
            }

        }
        #endregion
    }
    public enum ServerType { BIZTALK, SQL };
    public class Server
    {
        public string Name { get; set; }
        public ServerType Type { get; set; }
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
