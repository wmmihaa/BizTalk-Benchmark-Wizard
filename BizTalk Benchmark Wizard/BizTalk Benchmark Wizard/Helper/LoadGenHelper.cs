using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using LoadGen;
using System.Globalization;
using System.IO;
using System.Diagnostics;

namespace BizTalk_Benchmark_Wizard.Helper
{
    internal class LoadGenHelper
    {
        public delegate void CompleteHandler();
        public event CompleteHandler OnComplete;
        public List<PerfCounter> PerfCounters = new List<PerfCounter>();
        List<LoadGen.LoadGen> _loadGenClients = new List<LoadGen.LoadGen>();
        int _numberOfLoadGenStopped = 0;
        int _numberOfLoadGenClients = 0;
        public LoadGenHelper()
        {
            
        }
        public void RunTests(Environment environment, List<string> servers)
        {
            int n = _numberOfLoadGenClients;
            foreach (string server in servers)
            {
                if (n++ == environment.NuberOfActiveBizTalkServers)
                    break;

                CreateCounterCollectors(server);
            }

            foreach (string server in servers)
            {
                if (_numberOfLoadGenClients++ == environment.NuberOfActiveBizTalkServers)
                    break;

                _loadGenClients.Add(CreateAndStartLoadGenClient(CreateLoadGenScript(environment.LoadGenScripfile, server),server));
            }
        }
        public void StopAllTests()
        {
            foreach (LoadGen.LoadGen loadGen in _loadGenClients)
                loadGen.Stop();
        }
        private string CreateLoadGenScript(string template, string server)
        {
            string rootPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Resources\\LoadGenScripts");
            
            string newScriptFile=Path.Combine(rootPath, server+"_LoadGenScript.xml");
            if(File.Exists(newScriptFile))
                File.Delete(newScriptFile);
            
            StreamWriter writer =new StreamWriter(newScriptFile);

            using (StreamReader reader = new StreamReader(Path.Combine(rootPath, template))) 
            {
                while (reader.Peek() >= 0) 
                {
                    string newLine = reader.ReadLine();
                    newLine=newLine.Replace("@ServerName", server);
                    newLine=newLine.Replace("@FilePath", rootPath);
                    writer.WriteLine(newLine);
                }
            }
            writer.Close();
            return newScriptFile;
        }
        private LoadGen.LoadGen CreateAndStartLoadGenClient(string scriptFile, string server)
        {
            LoadGen.LoadGen loadGen = null;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(scriptFile);

                if (string.Compare(doc.FirstChild.Name, "LoadGenFramework", true, new CultureInfo("en-US")) != 0)
                {
                    throw new ConfigException("LoadGen Configuration File Schema Invalid!");
                }

                loadGen = new LoadGen.LoadGen(doc.FirstChild);
                loadGen.LoadGenStopped += new LoadGenEventHandler(LoadGen_Stopped);
                

                loadGen.Start();
            }
            catch (ConfigException cex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }

            //Thread.Sleep(0x1388);
            return loadGen;
        }
        private void CreateCounterCollectors(string server)
        {
            PerfCounter perfCounter = new PerfCounter();
            perfCounter.Server = server;

            perfCounter.ProcessedCounters.Add(new PerformanceCounter("BizTalk:Messaging", "Documents processed/Sec", "BBW_PxHost", server));
            perfCounter.ProcessedCounters.Add(new PerformanceCounter("BizTalk:Messaging", "Documents processed/Sec", "BBW_RxHost", server));
            perfCounter.ProcessedCounters.Add(new PerformanceCounter("BizTalk:Messaging", "Documents processed/Sec", "BBW_TxHost", server));

            perfCounter.ReceivedCounters.Add(new PerformanceCounter("BizTalk:Messaging", "Documents received/Sec", "BBW_PxHost", server));
            perfCounter.ReceivedCounters.Add(new PerformanceCounter("BizTalk:Messaging", "Documents received/Sec", "BBW_RxHost", server));
            perfCounter.ReceivedCounters.Add(new PerformanceCounter("BizTalk:Messaging", "Documents received/Sec", "BBW_TxHost", server));

            perfCounter.CPUCounters.Add(new PerformanceCounter("Processor", "% Processor Time", "_Total", server));
            
            PerfCounters.Add(perfCounter);
        }
        protected void RaiseCompleteEvent()
        {
            if (OnComplete != null)
            {
                OnComplete();
            }
        }
        private void LoadGen_Stopped(object sender, LoadGenStopEventArgs e)
        {
            //TimeSpan span1 = e.LoadGenStopTime.Subtract(e.LoadGenStartTime);
            //this._ctx.LogInfo("FilesSent: " + e.NumFilesSent);
            //this._ctx.LogInfo("StartTime: " + e.LoadGenStartTime);
            //this._ctx.LogInfo("StopTime:  " + e.LoadGenStopTime);
            //this._ctx.LogInfo("DeltaTime: " + span1.TotalSeconds + "Secs.");
            //this._ctx.LogInfo("Rate:      " + ((e.NumFilesSent) / span1.TotalSeconds));

            //bExitApp = true;
            
            _numberOfLoadGenStopped++;
            if (_numberOfLoadGenClients == _numberOfLoadGenStopped)
                RaiseCompleteEvent();
        }
    }
    public class PerfCounter
    {

        public List<PerformanceCounter> ProcessedCounters = new List<PerformanceCounter>();
        public List<PerformanceCounter> ReceivedCounters = new List<PerformanceCounter>();
        public List<PerformanceCounter> CPUCounters = new List<PerformanceCounter>();

        public float ProcessedCounterValue
        {
            get 
            {
                float ret = 0;
                foreach (PerformanceCounter c in this.ProcessedCounters)
                    ret += c.NextValue();
                return ret;
            }
        }
        public float ReceivedCounterValue
        {
            get
            {
                float ret = 0;
                foreach (PerformanceCounter c in this.ReceivedCounters)
                    ret += c.NextValue();
                return ret;
            }
        }
        public float CPUCounterValue
        {
            get
            {
                float ret = 0;
                foreach (PerformanceCounter c in this.CPUCounters)
                    ret += c.NextValue();
                return ret;
            }
        }
        public string Server = string.Empty;
    }
}
