using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Collections.ObjectModel;

namespace BizTalk_Benchmark_Wizard
{
    /// <summary>
    /// The ScenariosFactory is used to load scenarios
    /// </summary>
    internal class ScenariosFactory
    {
        public ScenariosFactory()
        {

        }
        /// <summary>
        /// Loads the Scenarios.xml file and returns the scenarios.
        /// </summary>
        /// <returns></returns>
        public static List<Scenario> Load()
        {
            try
            {
                string filePath = Path.Combine(
                    Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location), @"Resources\Scenarios.xml");
            
                if(!File.Exists(filePath))
                    throw new ApplicationException("[Scenarios.xml] were not found");

                XmlSerializer ser = new XmlSerializer(typeof(List<Scenario>));
                XmlReader reader = XmlReader.Create(filePath);
                return (List<Scenario>)ser.Deserialize(reader);
            }
            catch (Exception)
            {

                throw;
            }
        }
    }

    /// <summary>
    /// A Scenario represents a testing scenario, which could be executed. 
    /// Each Scenario has a number of Environments which should correlate with the environment
    /// that is going to be tested.
    /// </summary>
    public class Scenario
    {
        /// <summary>
        /// Name of the scenario. Eg "Messaging Single and Multi Message Box"
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Step by step description of whwat is going to happen.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// All the Environments on which the Senario has been tested
        /// </summary>
        public List<Environment> Environments = new List<Environment>();
    }
    public class Environment
    {
        /*
         <Environment>
            <Name>1 BTS with SQL</Name>
            <Description>Single server installation</Description>
            <LoadGenScriptFile>Orchestration Single Message Box_001.xml</LoadGenScriptFile>
            <NuberOfActiveBizTalkServers>1</NuberOfActiveBizTalkServers>
            <BizTalkServerConfiguration>1*CPU, 4GB RAM</BizTalkServerConfiguration>
            <NuberOfActiveSQLServers>0</NuberOfActiveSQLServers>
            <SqlServerConfiguration>1*CPU, 4GB RAM</SqlServerConfiguration>
            <MaxExpectedCpuUtilizationBizTalkRxHost>95</MaxExpectedCpuUtilizationBizTalkRxHost>
            <MaxExpectedCpuUtilizationBizTalkTxHost>95</MaxExpectedCpuUtilizationBizTalkTxHost>
            <MaxExpectedCpuUtilizationSql>45</MaxExpectedCpuUtilizationSql>
            <MinExpectedDocsProcessed>100</MinExpectedDocsProcessed>
            <MinExpectedDocsReceived>40</MinExpectedDocsReceived>
          </Environment>
         */

        /// <summary>
        /// Name of the environment. Eg. 1BTS + 1SQL
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Short desc
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The file path to the script file
        /// </summary>
        public string LoadGenScriptFile { get; set; }
        /// <summary>
        /// Number of BizTalk Servrs
        /// </summary>
        public int NumberOfActiveBizTalkServers { get; set; }
        /// <summary>
        /// BizTalk Server Configuration. Eg. 1*CPU, 4GB RAM
        /// </summary>
        public string BizTalkServerConfiguration { get; set; }
        /// <summary>
        /// Nuber Of Active SQLServers
        /// </summary>
        public int NumberOfActiveSqlServers { get; set; }
        /// <summary>
        /// SQL Server Configuration. Eg. 1*CPU, 4GB RAM
        /// </summary>
        public string SqlServerConfiguration { get; set; }
        /// <summary>
        /// Maximum Expected Cpu Utilization BizTalk
        /// </summary>
        public int MaxExpectedCpuUtilizationBizTalkRxHost { get; set; }
        /// <summary>
        /// Maximum Expected Cpu Utilization BizTalk
        /// </summary>
        public int MaxExpectedCpuUtilizationBizTalkTxHost { get; set; }
        /// <summary>
        /// Maximum Expected Cpu UtilizationSql
        /// </summary>
        public int MaxExpectedCpuUtilizationSql { get; set; }
        /// <summary>
        /// Minimum Expected Docs Processed
        /// </summary>
        public int MinExpectedDocsProcessed { get; set; }
        /// <summary>
        /// Minimum Expected Docs Received
        /// </summary>
        public int MinExpectedDocsReceived { get; set; }
    }
}
