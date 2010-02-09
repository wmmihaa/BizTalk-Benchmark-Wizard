using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LoadGen;
using System.Diagnostics;
using BizTalkBenchmarkWizard.PerformanceCounterHelper;

namespace BizTalkBenchmarkWizard.LoadGenArtefacts
{
    public class AppFabricServiceTransport : ITransport, IConfigurable
    {
        PerformanceCounterLogger _performanceCounterLogger = null;
        private AppFabricServiceReference.Test1Client client = new AppFabricServiceReference.Test1Client();
        private long _count;
        private string _configParameters;
        private string[] _dstLocation;
        private Message _messageToBeSent;
        public AppFabricServiceTransport()
        {
            _performanceCounterLogger = new PerformanceCounterLogger(PerformanceCounterLogger.ServiceType.Consumer);
            WriteTrace("constructor");
        }
        public void Cleanup(string sCleanup)
        {
            WriteTrace(sCleanup);
        }
        public string[] DstLocation
        {
            get
            {
                WriteTrace("");
                return _dstLocation;
            }
            set
            {
                string ret="";
                foreach (string s in value)
                    ret += s;
                WriteTrace(ret);

                _dstLocation = value;
            }
        }
        public void Initialize(string sInitialize)
        {
            WriteTrace(sInitialize);
        }
        public Message MessageToBeSent
        {
            get
            {
                WriteTrace("");
                return _messageToBeSent;
            }
            set
            {
                WriteTrace("");
                _messageToBeSent = value;
            }
        }
        public void SendLargeMessage(string UniqueDestFileName)
        {
            _count++;
            Trace.WriteLine("[BBW] Count=" + _count.ToString()); 
            
            WriteTrace(UniqueDestFileName);


            //object o = PerformanceCounterLogger.UpdateEntryCounters("Transmitted msgs/sec");
            client.GetData(4);
            //PerformanceCounterLogger.UpdateExitCounters(o);
        }
        public void SendSmallMessage(string UniqueDestFileName)
        {
            _count++;
            Trace.WriteLine("[BBW] Count="+_count.ToString());
            WriteTrace(UniqueDestFileName);

            _performanceCounterLogger.UpdateTransmitEntryCounters();
            try
            {
                client.GetData(4);
            }
            catch 
            {
                client = new AppFabricServiceReference.Test1Client();
                Trace.WriteLine("[BBW] Re-initiating the client");
            }
            finally
            {
                _performanceCounterLogger.UpdateTransmitExitCounters();
            }
        }

        private void WriteTrace(string msg)
        {
            try
            {
                Trace.WriteLine("[BBW] " + new System.Diagnostics.StackFrame(1).GetMethod().Name + "[" + msg + "]");
            }
            catch
            {
                Trace.WriteLine("[BBW] " + "Unable to retrieve method name");
            }
        }

        public string ConfigParameters
        {
            get
            {
                WriteTrace(_configParameters);
                return _configParameters;
            }
            set
            {
                WriteTrace(_configParameters);
                _configParameters = value;
            }
        }
 
    }
}
