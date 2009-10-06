using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using LoadGen;
using System.Globalization;

namespace BizTalk_Benchmark_Wizard.Helper
{
    internal class LoadGenHelper
    {
        public void RunTest()
        {
            Exit = false;

        }
        public bool Exit { get; set; }
        public void Execute(string loadGenTestConfig)//XmlNode testConfig, Context context)
        {
         
            //try
            //{
            //    XmlDocument doc = new XmlDocument();
            //    doc.Load(loadGenTestConfig);

            //    if (string.Compare(doc.FirstChild.Name, "LoadGenFramework", true, new CultureInfo("en-US")) != 0)
            //    {
            //        throw new ConfigException("LoadGen Configuration File Schema Invalid!");
            //    }

            //    LoadGen.LoadGen loadGen = new LoadGen.LoadGen(doc.FirstChild);
            //    loadGen.LoadGenStopped += new LoadGenEventHandler(this.LoadGen_Stopped);
            //    loadGen.Start();
            //}
            //catch (ConfigException cex)
            //{
            //    throw;
            //}
            //catch (Exception ex)
            //{
            //    throw;
            //}

            //while (!Exit)
            //{
            //    Thread.Sleep(0x3e8);
            //}
            //Thread.Sleep(0x1388);
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
        }
    }
}
