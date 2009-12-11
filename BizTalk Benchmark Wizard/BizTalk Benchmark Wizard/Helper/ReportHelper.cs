using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Reporting.WinForms;
using Microsoft.Win32;
using BizTalk_Benchmark_Wizard.Reports;

namespace BizTalk_Benchmark_Wizard.Helper
{
    class ReportHelper
    {
        public static void CreateReport(List<TestResult> testResults, string scenarioName)
        {
            string reportType = "PDF";
            string mimeType;
            string encoding;
            string fileNameExtension;
            Warning[] warnings;
            string[] streams;
            byte[] renderedBytes;

            string deviceInfo =
             "<DeviceInfo>" +
             "  <OutputFormat>PDF</OutputFormat>" +
             "  <PageWidth>8.5in</PageWidth>" +
             "  <PageHeight>11in</PageHeight>" +
             "  <MarginTop>0.5in</MarginTop>" +
             "  <MarginLeft>1in</MarginLeft>" +
             "  <MarginRight>1in</MarginRight>" +
             "  <MarginBottom>0.5in</MarginBottom>" +
             "</DeviceInfo>";

            ReportViewer reportViewer = new ReportViewer();
            reportViewer.LocalReport.ReportPath = "TestResultReport.rdlc";
            ReportDataSource ds = new ReportDataSource("TestResult", testResults);

            reportViewer.LocalReport.DataSources.Add(ds);
            reportViewer.RefreshReport();

            renderedBytes = reportViewer.LocalReport.Render(
                reportType,
                deviceInfo,
                out mimeType,
                out encoding,
                out fileNameExtension,
                out streams,
                out warnings);

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".pdf";
            dlg.Filter = "Pdf documents (.pdf)|*.pdf";
            //string fileName =  string.Format("{0}_{1}.pdf", scenarioName, DateTime.Now.ToString()).Replace(" ", "_");
            //dlg.FileName = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location,fileName);
            if (dlg.ShowDialog() == true)
            {
                string filename = dlg.FileName;

                if (File.Exists(filename))
                    File.Delete(filename);

                FileStream stream = File.Open(filename, FileMode.CreateNew);
                Stream fromStream = new MemoryStream(renderedBytes);
                byte[] buffer = new byte[4096];
                int i = 0;

                while ((i = fromStream.Read(buffer, 0, 4096)) > 0)
                    stream.Write(buffer, 0, i);

                stream.Flush();
                stream.Close();
                fromStream.Close();
            }
        }
    }
}
