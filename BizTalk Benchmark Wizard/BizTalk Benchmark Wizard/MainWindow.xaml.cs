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
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using BizTalk_Benchmark_Wizard.Helper;
using System.Timers;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

namespace BizTalk_Benchmark_Wizard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constants
        const int TIMERTICKS = 10000;
        const int TESTRUNFORNUMBEROFMINUTES = 2;
        #endregion
        #region Private Members
        bool _isLogedIn = false;
        List<Scenario> _scenarios;
        BizTalkHelper _bizTalkHelper = null;
        PerflogHelper _perflogHelper = null;
        LoadGenHelper _loadGenHelper = null;
        System.Timers.Timer _timer = null;
        DateTime _testStartTime;
        long _avgCpuValue = 0;
        long _avgProcessedValue = 0;
        long _avgRreceivedValue = 0;
        float _timerCount = 0;
        
        #endregion
        #region Public Members
        public IEnumerable<Environment> Environments;
        public List<Result> Results = new List<Result>();
        public int ProcessValue
        {
            set
            {
                int newValue = value;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate { SetValue(ProcessValueProperty, newValue); }, value);
            }
            get
            {
                int processValue = (int)this.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    (DispatcherOperationCallback)delegate { return GetValue(ProcessValueProperty); }, ProcessValueProperty);

                return processValue;
            }
        }
        
        public static readonly DependencyProperty ProcessValueProperty = DependencyProperty.Register(
                                                                            "ProcessValue",
                                                                            typeof(int),
                                                                            typeof(MainWindow),
                                                                            new PropertyMetadata(0));
        #endregion
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion
        #region Events
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadScenarions();
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            switch (tabControl1.SelectedIndex)
            { 
                case 0:
                    if (!_isLogedIn)
                    {
                        btnNext.IsEnabled = false;
                        PopupLogin.IsOpen = true;
                        return;
                    }
                   break;
                case 1: 
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    PrepareTest();
                    break;
                case 5:
                    ShowResult();
                    break;
                case 6: 
                    this.Close();
                    break;
            }
            
            tabControl1.SelectedIndex++;

        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            //if (tabControl1.SelectedIndex > 0)
            //    btnBack.Visibility = Visibility.Visible;
            //else
            //    btnBack.Visibility = Visibility.Hidden;

            //if (tabControl1.SelectedIndex > 5)
            //    btnNext.Visibility = Visibility.Hidden;
            //else
            //    btnNext.Visibility = Visibility.Visible;

            tabControl1.SelectedIndex--;
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Cursor = Cursors.Wait;
            btnOk1.IsEnabled = false;
            btnCancel1.IsEnabled = false;
            PopupLogin.IsOpen = false;
            PopupLogin.UpdateLayout();
            try
            {
                RefreshPreRequsites();
                btnNext.IsEnabled = true;
                tabControl1.SelectedIndex++;
            }
            catch (Exception ex)
            {
                string msg = "There where a problem connecting to the database.\nMessage:\n" + ex.Message;
                if (ex.InnerException != null)
                    msg += "\n\nInner Exception:\n" + ex.InnerException.Message;
                txtException.Text = msg;
                PopupException.IsOpen = true;
            }
            finally { this.Cursor = null; }
        }
        private void btnOk2_Click(object sender, RoutedEventArgs e)
        {
            PopupServiceAccountAndGroups.IsOpen = false;
            foreach (Server server in _bizTalkHelper.GetServers(txtServer1.Text, txtMgmtDb1.Text).Where(s => s.Type == ServerType.BIZTALK))
            {
                _bizTalkHelper.CreateBizTalkHosts(server.Name, txtWindowsGroup.Text, txtServiceAccount.Text, txtPasswrod.Password);
            }
        }
        private void btnExceptionOk_Click(object sender, RoutedEventArgs e)
        {
            PopupException.IsOpen = false;
        }
        private void btnGenerateReport_Click(object sender, RoutedEventArgs e)
        {

        }
        private void btnTestService_Click(object sender, RoutedEventArgs e)
        {
            _loadGenHelper = new LoadGenHelper();
            bool testPass = _loadGenHelper.TestIndigoService("localhost");
            picServiceIsRunning.Source = testPass ?
                       new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png")) :
                       new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/failed.png"));

            btnNext.IsEnabled = testPass;
        }
        private void btnCreateHosts_Click(object sender, RoutedEventArgs e)
        {
            PopupServiceAccountAndGroups.IsOpen = true;                    
        }
        private void btnCreateCollectors_Click(object sender, RoutedEventArgs e)
        {
            _perflogHelper.CreateDataCollectorSets();
        }
        private void cbScenario_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string item = (string)e.AddedItems[0];

            if (!string.IsNullOrEmpty(item))
                Environments = _scenarios.First(s => s.Name == item).Environments;

            ScenarioDescription.Text = _scenarios.First(s => s.Name == item).Description;
            ScenarioName.Text = _scenarios.First(s => s.Name == item).Name;

            if (item.StartsWith("Messaging"))
            {
                ScenairoPicture1.Visibility = Visibility.Visible;
                ScenairoPicture2.Visibility = Visibility.Hidden;
            }
            else
            {
                ScenairoPicture1.Visibility = Visibility.Hidden;
                ScenairoPicture2.Visibility = Visibility.Visible;
            }
            this.environments.DataContext = Environments;

        }
        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            switch (((TabControl)sender).SelectedIndex)
            {
                case 0:
                    btnBack.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    btnBack.Visibility = Visibility.Visible;
                    break;
                case 2:
                    break;
                case 3:
                    btnNext.Content = "Next";
                    btnNext.IsEnabled = true;
                    break;
                case 4:
                    // Prepare
                    btnNext.Content = "Run Test";
                    btnNext.IsEnabled = false;
                    break;
                case 5:
                    //Run test
                    btnNext.Content = "Next";
                    btnBack.Visibility = Visibility.Visible;
                    btnNext.Visibility = Visibility.Visible;
                    break;
                case 6:
                    //Show result
                    btnNext.Content = "Close";
                    btnBack.Visibility = Visibility.Visible;
                    btnNext.Visibility = Visibility.Visible;
                    break;


            }

        }
        #endregion
        #region Private Methods
        void ShowResult()
        {
            Results.Clear();

            // Hard coded for demo purpose. This information should be collected from the test result
            Results.Add(new Result() { CouterName = "Avg Processor time (SQL)", TestValue = "50", KPI = "< 60", Status = "Succeeded" });
            Results.Add(new Result() { CouterName = "Avg Processor time (BizTalk)", TestValue = "89", KPI = "< 90", Status = "Succeeded" });
            Results.Add(new Result() { CouterName = "Avg Processed msgs / sec (*)", TestValue = "344", KPI = "> 500", Status = "Failed" });
            ResultGrid.DataContext = Results;
        }
        void RefreshPreRequsites()
        {
            _bizTalkHelper = new BizTalkHelper(txtServer1.Text, txtMgmtDb1.Text);
            _perflogHelper = new PerflogHelper(_bizTalkHelper.GetServers(txtServer1.Text, txtMgmtDb1.Text));

            picInstallCollectorSet.Source = _perflogHelper.IsDataCollectorSetsCreated ?
                       new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png")) :
                       new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/checklist.png"));

            btnCreateCollectors.Visibility = _perflogHelper.IsDataCollectorSetsCreated ? Visibility.Hidden : Visibility.Visible;

            picInstallHost.Source = _bizTalkHelper.IsBizTalkHostsInstalled ?
                new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png")) :
                new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/checklist.png"));

            btnCreateHosts.Visibility = _bizTalkHelper.IsBizTalkHostsInstalled ? Visibility.Hidden : Visibility.Visible;

            picInstalledScenario.Source = _bizTalkHelper.IsBizTalkScenariosInstalled ?
                new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png")) :
                new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/checklist.png"));

            if (_bizTalkHelper.IsBizTalkHostsInstalled && _bizTalkHelper.IsBizTalkScenariosInstalled)
            {
                btnNext.Visibility = Visibility.Visible;
                lblInstalledScenarioManual.Visibility = Visibility.Hidden;
            }
            else
                btnNext.Visibility = Visibility.Collapsed;
            
        }
        void LoadScenarions()
        {

            try
            {
                _scenarios = ScenariosFactory.Load();

                foreach (var scenario in _scenarios)
                    cbScenario.Items.Add(scenario.Name);

                cbScenario.SelectedIndex = 0;

                Environments = _scenarios[0].Environments;

                this.environments.DataContext = Environments;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unable to load Senarios configuration", ex);
            }
        }
        #endregion
        #region Run Tests
        void PrepareTest() 
        {
            // Configure tests...

            btnBack.Visibility = Visibility.Hidden;
            btnNext.Visibility = Visibility.Hidden;
            _testStartTime = DateTime.Now;
            _avgCpuValue = 0;
            _avgProcessedValue = 0;
            _avgRreceivedValue = 0;
            _timerCount = 0;
            CPUGauge.MaxValue = 180;
            ProcessedGauge.MaxValue = 450;
            ReceivedGauge.MaxValue = 450;
            ProcessValue = 0;
            Progress.DataContext = this;
            
            RunTest();
        }
        void RunTest()
        {
            _loadGenHelper.RunTests((Environment)environments.SelectedItem, _bizTalkHelper.GetApplicationServerNames());
            _loadGenHelper.OnComplete += new LoadGenHelper.CompleteHandler(_loadGenHelper_OnComplete);
            _timer = new System.Timers.Timer(TIMERTICKS);
            _timer.Elapsed += new ElapsedEventHandler(_timer_CollectData);
            _timer.Start();
            
        }

        void _loadGenHelper_OnComplete()
        {
            _timer.Stop();
            //btnNext.Visibility = Visibility.Visible;
        }
        void _timer_CollectData(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            _timerCount++;
            float cpuValue=0;
            float processedValue=0;
            float receivedValue=0;
            
            foreach (PerfCounter c in _loadGenHelper.PerfCounters)
            {
                cpuValue += c.CPUCounterValue;
                processedValue += c.ProcessedCounterValue;
                receivedValue += c.ReceivedCounterValue;
            }

            _avgCpuValue = (long)((_avgCpuValue + cpuValue) * _timerCount);
            _avgProcessedValue = (long)((_avgProcessedValue + processedValue) * _timerCount);
            _avgRreceivedValue = (long)((_avgRreceivedValue + receivedValue) * _timerCount);

            // Set gauge values
            CPUGauge.SetCounter((int)cpuValue, (int)(_avgCpuValue / _timerCount));
            ProcessedGauge.SetCounter( (int)processedValue, (int)(_avgProcessedValue / _timerCount));
            ReceivedGauge.SetCounter((int)receivedValue, (int)(_avgRreceivedValue / _timerCount));

            _timer.Enabled = true;
        }
        
        #endregion

        
    }
    /// <summary>
    /// Used for presenting the test result
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Eg "Avg Processed msgs / sec (*)
        /// </summary>
        public string CouterName { get; set; }
        /// <summary>
        /// Test result
        /// </summary>
        public string TestValue { get; set; }
        /// <summary>
        /// Value collected from the Scenarios 
        /// </summary>
        public string KPI { get; set; }
        /// <summary>
        /// Sucess / Failed
        /// </summary>
        public string Status { get; set; }
    }
    
}
