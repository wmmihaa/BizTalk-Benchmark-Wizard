﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace BizTalk_Benchmark_Wizard.Helper
{
    /// <summary>
    /// This helper class is a collection of all fuctionality required for working with Perfmon
    /// </summary>
    internal class PerflogHelper
    {
        List<Server> _servers = null;
        public PerflogHelper(List<Server> servers)
        {
            _servers = servers;
        }

        /// <summary>
        /// Checks if collector sets are installed from all servers
        /// </summary>
        public bool IsDataCollectorSetsCreated
        {
            get 
            {
                foreach (Server server in _servers)
                { 
                    string collectorsetName = server.Type==ServerType.BIZTALK?server.Name+"_BizTalk Server":server.Name+"_SQL Server";

                    if (!IsDataCollectorSetCreatedForServer(collectorsetName))
                        return false;
                }
                return true;
            }
        }
        /// <summary>
        /// Creates collector sets for all serveres
        /// </summary>
        public void CreateDataCollectorSets()
        {
            string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (Server server in _servers)
            {
                try
                {
                    if (server.Type == ServerType.BIZTALK)
                        CreateDataCollectorSetForServer(server.Name, Path.Combine(basePath, @"Resources\Collector Set Templates\BizTalk Server.xml"));
                    else
                        CreateDataCollectorSetForServer(server.Name, Path.Combine(basePath, @"Resources\Collector Set Templates\SQL Server.xml"));
                }
                catch (Exception ex)
                {

                }
            }
        }
        /// <summary>
        /// Checks if a collector set is created for a specific server
        /// </summary>
        /// <param name="collectorsetName"></param>
        /// <returns></returns>
        private bool IsDataCollectorSetCreatedForServer(string collectorsetName)
        {
            return false;

            // This does not work at the moment...
            using (ProcessHelper p = new ProcessHelper())
            {
                string format = string.Format(@"query ""{0}""", collectorsetName);
                p.Execute("logman", format, 10000);
            }
            Thread.Sleep(100);
            if (!string.IsNullOrEmpty(ProcessHelper.ErrorMessage) || ProcessHelper.OutPutMessage.ToUpper().Contains("ERROR"))
                return false;

            return true;
        }
        /// <summary>
        /// Creates a collector set for  a specific server
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="template"></param>
        private void CreateDataCollectorSetForServer(string serverName, string template)
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
                p.Execute("logman", format, 60000);
            }
            Thread.Sleep(100);
            if (!string.IsNullOrEmpty(ProcessHelper.ErrorMessage) || !string.IsNullOrEmpty(ProcessHelper.OutPutMessage))
                throw new ApplicationException("Unable to create Data Collector Set");
        }
        

    }
}