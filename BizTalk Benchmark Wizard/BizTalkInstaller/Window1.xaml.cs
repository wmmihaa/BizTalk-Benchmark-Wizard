using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;

namespace BizTalkInstaller
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        BizTalkHelper _btsHelper = null;
        Uri _passedUri = new Uri("pack://application:,,,/BizTalkInstaller;component/Images/passed.png");
        Uri _runningUri = new Uri("pack://application:,,,/BizTalkInstaller;component/Images/gear_run.png");
                   
        public Window1()
        {
            InitializeComponent();
            
        }
        List<string> _bizTalkServers
        {
            get 
            {
                List<String> servers = new List<string>();
                servers.Add(cbRxHost.Text);

                if (!servers.Contains(cbPxHost.Text))
                    servers.Add(cbPxHost.Text);

                if (!servers.Contains(cbTxHost.Text))
                    servers.Add(cbTxHost.Text);

                return servers;
            }
        
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (btnNext.Content != "Next")
                this.Close();

            tabControl1.SelectedIndex++;
        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            tabControl1.SelectedIndex--;
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;

        }
        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnNext.Content = tabControl1.SelectedIndex == tabControl1.Items.Count - 1 ? "Close" : "Next";
            btnBack.Visibility = tabControl1.SelectedIndex <= 1 ? Visibility.Hidden : Visibility.Visible;

            switch (tabControl1.SelectedIndex)
            { 
                case 2:
                    if (_btsHelper == null)
                    {
                        _btsHelper = new BizTalkHelper(txtServer.Text, txtMgmtDb.Text);
                        _btsHelper.OnStepComplete += new BizTalkHelper.InitiateStepHandler(_btsHelper_OnStepComplete);
                    }
                    foreach (string server in _btsHelper.GetApplicationServerNames())
                    {
                        cbPxHost.Items.Add(server);
                        cbRxHost.Items.Add(server);
                        cbTxHost.Items.Add(server);
                    }
                    cbPxHost.SelectedIndex = 0;
                    cbRxHost.SelectedIndex = 0;
                    cbTxHost.SelectedIndex = 0;
                    break;
                case 3:
                    DoEvents();
                    picHosts.Source = new BitmapImage(_runningUri);
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() 
                        {
                            _btsHelper_OnStepComplete(null, new StepEventArgs() { EventStep = "Start" });
                        }));
                    
                    break;
            }
        }

        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
        }
        void  _btsHelper_OnStepComplete(object sender, StepEventArgs e)
        {
            DoEvents();
            switch (e.EventStep)
            {
                case "Start":
                    _btsHelper.CreateBizTalkHosts(_bizTalkServers, txtNtGroupName.Text, (bool)chbStopAllHosts.IsChecked);
                    break;
                case "CreateBizTalkHosts":
                    picHosts.Source = new BitmapImage(_passedUri);
                    picInstances.Source = new BitmapImage(_runningUri);
                    
                    _btsHelper.CreateBizTalkHostInstancess(e.Servers, txtUserName.Text, txtPassword.Password);
                    break;
                case "CreateBizTalkHostInstancess":
                    picInstances.Source = new BitmapImage(_passedUri);
                    picHandlers.Source = new BitmapImage(_runningUri);

                    _btsHelper.CreateBizTalkHostHandlers(e.Servers);
                    break;
                case "CreateBizTalkHostHandlers":
                    picHandlers.Source = new BitmapImage(_passedUri);
                    picArtifacts.Source = new BitmapImage(_runningUri);

                    _btsHelper.InstallBizTalkArtifacts(e.Servers);
                    break;
                case "InstallBizTalkArtifacts":
                    picArtifacts.Source = new BitmapImage(_passedUri);
                    picStartsInstances.Source = new BitmapImage(_runningUri);

                    _btsHelper.UpdateRegistrySettings(e.Servers);
                    break;
                case "UpdateRegistrySettings":

                    _btsHelper.StartBizTalkHostInstances(e.Servers);
                    break;
                case "StartBizTalkHostInstances":
                    picStartsInstances.Source = new BitmapImage(_passedUri);

                    tabControl1.SelectedIndex++;
                    break;
            }
        }


        private void picHosts_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            
        }

        private void picHosts_FocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }        
    }
}
