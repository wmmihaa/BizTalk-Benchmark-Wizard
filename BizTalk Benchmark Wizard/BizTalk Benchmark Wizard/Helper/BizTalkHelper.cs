using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizTalk_Benchmark_Wizard.Helper
{
    internal class BizTalkHelper
    {
        public List<Server> GetServers(string server, string mgmtDatabase)
        {
            return null;
        }
        public bool IsBizTalkScenariosInstalled
        {
            get { return true; }
        }
        public bool IsBizTalkHostsInstalled
        {
            get { return true; }
        }
        public void CreateBizTalkHosts(string group, string username, string password)
        { 
        
        }

    }
    internal enum ServerType{BIZTALK, SQL};
    internal class Server
    {
        public string Name{get;set;}
        public ServerType Type {get;set;}
    }
}
