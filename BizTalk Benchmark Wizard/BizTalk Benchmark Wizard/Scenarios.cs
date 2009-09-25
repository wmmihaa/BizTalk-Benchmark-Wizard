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
    public class ScenariosFactory
    {
        const string FILENAME = "AAA.xml";
        public static void Save(IEnumerable<Scenario> scenarios)
        {
            XmlSerializer ser = new XmlSerializer(typeof(IEnumerable<Scenario>));
            XmlWriter writer = XmlWriter.Create(FILENAME);
            ser.Serialize(writer, scenarios);
            writer.Close();
        }
        public static List<Scenario> Load()
        {
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<Scenario>));
                XmlReader reader = XmlReader.Create(@"C:\Users\Administrator\Documents\Visual Studio 2008\Projects\BizTalk Benchmark Wizard\Resources\Scenarios.xml");
                return (List<Scenario>)ser.Deserialize(reader);
            }
            catch (Exception ex)
            {
                
                throw;
            } 
        }
    }
    public class Environment
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int NuberOfActiveBizTalkServers { get; set; }
        public string BizTalkServerConfiguration { get; set; }
        public int NuberOfActiveSQLServers { get; set; }
        public string SQLServerConfiguration { get; set; }
        public int MaxExpectedCpuUtilizationBizTalk { get; set; }
        public int MaxExpectedCpuUtilizationSql { get; set; }
        public int MinExpectedDocsProcessed { get; set; }
        public int MinExpectedDocsReceived { get; set; }
    }
    public class Scenario
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Environment> Environments = new List<Environment>();
    }
}
