using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace BizTalk_Benchmark_Wizard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ((MainWindow)this.Windows[0]).txtException.Text = e.Exception.Message;
            ((MainWindow)this.Windows[0]).PopupException.IsOpen = true;
            e.Handled=true;
        }
    }
}
