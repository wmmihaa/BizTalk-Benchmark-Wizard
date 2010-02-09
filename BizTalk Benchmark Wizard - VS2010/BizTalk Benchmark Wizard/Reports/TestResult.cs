using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizTalk_Benchmark_Wizard.Reports
{
    public class TestResult
    {
        public string Scenario { get; set; }
        public DateTime TestDate { get; set; }
        public string TestDuration { get; set; }
        public string Result { get; set; }
        public string TestDescription { get; set; }
        public int NumberOfSqlServers { get; set; }
        public string SqlConfiguration { get; set; }
        public int NumberOfBizTalkServers { get; set; }
        public string BizTalkConfiguration { get; set; }

        public string CounterName { get; set; }
        public string TestValue { get; set; }
        public string Kpi { get; set; }
        public string Status { get; set; }
    }
}
