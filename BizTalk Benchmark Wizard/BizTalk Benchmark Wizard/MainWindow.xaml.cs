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
using System.Windows.Threading;
using System.Collections.ObjectModel;
using BizTalk_Benchmark_Wizard.Helper;
using System.Timers;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Configuration;

namespace BizTalk_Benchmark_Wizard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window ,IDisposable
    {
        #region Constants
        const int TIMERTICKS = 5000; // Refresh interval for gauges
        #endregion
        #region Private Members
        bool _hasRefreshed;
        bool _isLogedIn;
        List<Scenario> _scenarios;
        BizTalkHelper _bizTalkHelper;
        PerflogHelper _perflogHelper;
        LoadGenHelper _loadGenHelper;
        System.Timers.Timer _timer;
        DateTime _testStartTime;
        long _avgCpuValue;
        long _avgProcessedValue;
        long _avgRreceivedValue;
        long _totalCpuValue;
        long _totalProcessedValue;
        long _totalRreceivedValue;
        long _timerCount;
        bool _isWarmingUp = true;
        IEnumerable<Environment> Environments;
        List<HostMaping> HostMappings;
        List<Result> Results = new List<Result>();
        int ProcessValue
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
        static readonly DependencyProperty ProcessValueProperty = DependencyProperty.Register(
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
                        txtServer1.Text = Properties.Settings.Default.DefaultServer;
                        txtIndigoServiceServer.Text = Properties.Settings.Default.DefaultIndigoServiceUri;
                        btnNext.IsEnabled = false;
                        PopupLogin.IsOpen = true;
                        _isLogedIn = true;
                        return;
                    }
                   break;
                case 1:
                    //Auto selecting environment
                    int numberOfBizTalkServers = 0;
                    int numberOfSQLServers = 0;
                    foreach (Server server in _bizTalkHelper.GetServers(txtServer1.Text, txtMgmtDb1.Text).Where(s => s.Type == ServerType.BIZTALK))
                    {
                        if (server.Type == ServerType.BIZTALK)
                            numberOfBizTalkServers++;
                        else
                            numberOfSQLServers++;
                    }

                    if (numberOfBizTalkServers + numberOfSQLServers == 1)
                        environments.SelectedIndex = 0;
                    else if (numberOfBizTalkServers == 1 && numberOfSQLServers == 1)
                        environments.SelectedIndex = 1;
                    else
                        environments.SelectedIndex = 2;
                   btnNext.IsEnabled = false;
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    PreRunTest();
                    DoEvents();
                    PrepareTest();
                    break;
                case 6:
                    break;
                case 7: 
                    this.Close();
                    break;
            }
            
            tabControl1.SelectedIndex++;

        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            tabControl1.SelectedIndex--;
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            PopupLogin.IsOpen = false;
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
            btnTestService.IsEnabled = false;
            Properties.Settings.Default.DefaultIndigoServiceUri = txtIndigoServiceServer.Text;
            Properties.Settings.Default.Save();

            _loadGenHelper = new LoadGenHelper();
            bool testPass = _loadGenHelper.TestIndigoService(txtIndigoServiceServer.Text);
            picServiceIsRunning.Source = testPass ?
                       new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png")) :
                       new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/failed.png"));

            btnNext.IsEnabled = testPass;
            btnTestService.IsEnabled = true;
        }
        private void btnCreateCollectors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { PreCreatingCollectorSet(); }));
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { _perflogHelper.CreateDataCollectorSets(); }));
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { RefreshPreRequsites(); }));
            }
            catch { throw; }
            finally
            {
                WaitPanel.Visibility = Visibility.Hidden;
                btnBack.IsEnabled = true;
                btnNext.IsEnabled = true;
                btnCreateCollectors.IsEnabled = true;
            }
        }
        private void cbScenario_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string item = (string)e.AddedItems[0];

            if (!string.IsNullOrEmpty(item))
                Environments = _scenarios.First(s => s.Name == item).Environments;

            ScenarioDescription.Text = _scenarios.First(s => s.Name == item).Description;
            ScenarioName.Text = _scenarios.First(s => s.Name == item).Name;

            if (item.StartsWith("Messaging",StringComparison.CurrentCulture))
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
        private void PopupLogin_Closed(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Wait;
            btnBack.IsEnabled = false;
            btnNext.IsEnabled = false;
            Properties.Settings.Default.DefaultServer = txtServer1.Text;
            Properties.Settings.Default.Save();
            tabControl1.SelectedIndex++;
        }
        private void environments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (environments.SelectedItems.Count == 1)
                btnNext.IsEnabled = true;
        }
        private void environments_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (environments.SelectedItems.Count == 1)
                btnNext.IsEnabled = true;
        }
        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((TabControl)sender).SelectedIndex)
            {
                case 0:
                    btnBack.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { RefreshPreRequsites(); }));
                    btnBack.Visibility = Visibility.Visible;
                    break;
                case 2:
                    break;
                case 3:
                    btnNext.Content = "Next";
                    btnNext.IsEnabled = true;
                    break;
                case 4:
                    break;
                case 5:
                    // Prepare
                    btnNext.Content = "Run Test";
                    btnNext.IsEnabled = false;
                    break;
                case 6:
                    //Run test
                    btnNext.Content = "Next";
                    btnBack.IsEnabled = false;
                    btnNext.IsEnabled = false;
                    break;
                case 7:
                    //Show result
                    ShowResult();

                    btnNext.Content = "Close";
                    btnBack.IsEnabled = false;
                    btnNext.IsEnabled = true;
                    break;
            }
        }
        #endregion
        #region Private Methods
        void ShowResult()
        {
            Results.Clear();

            Environment environment = (Environment)environments.SelectedItem;
            bool cpuSuccess = _avgCpuValue < (long)environment.MaxExpectedCpuUtilizationBizTalk ? true : false;
            bool processedSuccess = _avgProcessedValue > (long)environment.MinExpectedDocsProcessed ? true : false;
            bool receivedSuccess = _avgRreceivedValue > (long)environment.MinExpectedDocsReceived ? true : false;

            // CPU
            Results.Add(new Result()
            {
                CounterName = "Avg Processor time (BizTalk)",
                TestValue = _avgCpuValue.ToString(CultureInfo.InvariantCulture),
                Kpi = "<" + environment.MaxExpectedCpuUtilizationBizTalk.ToString(),
                Status = cpuSuccess ? "Succeeded" : "Failed"
            });
            //Processed
            Results.Add(new Result()
            {
                CounterName = "Avg Processed msgs / sec (*)",
                TestValue = _avgProcessedValue.ToString(CultureInfo.InvariantCulture),
                Kpi = ">" + environment.MinExpectedDocsProcessed.ToString(),
                Status = processedSuccess ? "Succeeded" : "Failed"
            });
            //Processed
            Results.Add(new Result()
            {
                CounterName = "Avg Received msgs / sec (*)",
                TestValue = _avgRreceivedValue.ToString(CultureInfo.InvariantCulture),
                Kpi = ">" + environment.MinExpectedDocsReceived.ToString(),
                Status = receivedSuccess ? "Succeeded" : "Failed"
            });
            ResultGrid.DataContext = Results;

            if (cpuSuccess && processedSuccess && receivedSuccess)
            {
                lblSucess.Text = "Succeeded";
                picSucess.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png"));
            }
           
        }
        void RefreshPreRequsites()
        {
            if (_hasRefreshed)
                return;
            try
            {
                _bizTalkHelper = new BizTalkHelper(txtServer1.Text, txtMgmtDb1.Text);
                _perflogHelper = new PerflogHelper(_bizTalkHelper.GetServers(txtServer1.Text, txtMgmtDb1.Text));

                bool isDataCollectorSetsCreated = _perflogHelper.IsDataCollectorSetsCreated;

                DoEvents();
                picInstallCollectorSet.Source = isDataCollectorSetsCreated ?
                           new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png")) :
                           new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/checklist.png"));

                btnCreateCollectors.Visibility = isDataCollectorSetsCreated ? Visibility.Hidden : Visibility.Visible;
                btnCreateCollectors.IsEnabled = isDataCollectorSetsCreated ? false : true;
                chbStartCollectorSets.IsEnabled = isDataCollectorSetsCreated ? true : false;

                bool isBizTalkScenariosInstalled = _bizTalkHelper.IsBizTalkScenariosInstalled;
                DoEvents();
                picInstalledScenario.Source = isBizTalkScenariosInstalled ?
                    new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/passed.png")) :
                    new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/checklist.png"));

                List<string> btsServers = new List<string>();
                foreach (Server s in _bizTalkHelper.GetServers(txtServer1.Text, txtMgmtDb1.Text).Where(s => s.Type == ServerType.BIZTALK))
                    btsServers.Add(s.Name);


                HostMappings = _bizTalkHelper.GetHostMappings();
                DoEvents();
                this.lstHosts.DataContext = (IEnumerable<HostMaping>) HostMappings;

                WaitPanel.Visibility = Visibility.Collapsed;

                if (isBizTalkScenariosInstalled)
                {
                    btnNext.IsEnabled = true;
                    lblInstalledScenarioManual.Visibility = Visibility.Hidden;
                }
                else
                    InstallInstructions.Visibility = Visibility.Visible;

                btnBack.IsEnabled = true;
                _hasRefreshed = true;
            }
            catch (Exception ex)
            {
                string msg = "There where a problem connecting to the database.\nMessage:\n" + ex.Message;
                if (ex.InnerException != null)
                    msg += "\n\nInner Exception:\n" + ex.InnerException.Message;
                txtException.Text = msg;
                PopupException.IsOpen = true;
            }
            finally
            {
                this.Cursor = null;
            }
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

        void PreCreatingCollectorSet()
        {
            lblWait.Text = "Please wait while creating Perfmon collector sets...";
            WaitPanel.Visibility = Visibility.Visible;
            btnBack.IsEnabled = false;
            btnNext.IsEnabled = false;
            btnCreateCollectors.IsEnabled = false;
            this._hasRefreshed = false;
        }
        void PreRunTest()
        {
            //btnBack.IsEnabled = false;
            //btnNext.IsEnabled = false;
            //btnCreateCollectors.IsEnabled = false;
            //btnTestService.IsEnabled = false;
            //txtIndigoServiceServer.IsEnabled = false;

            btnNext.Visibility = Visibility.Hidden;
            btnBack.Visibility = Visibility.Hidden;
        }
        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
        }

        #endregion
        #region Run Tests
        private void PrepareTest() 
        {
            _bizTalkHelper.OnStepComplete += new BizTalkHelper.InitiateStepHandler(OnStepComplete);
            _loadGenHelper.OnStepComplete+=new LoadGenHelper.InitiateStepHandler(OnStepComplete);
            _perflogHelper.OnStepComplete+=new PerflogHelper.InitiateStepHandler(OnStepComplete);
            _loadGenHelper.OnComplete += new LoadGenHelper.CompleteHandler(OnTestComplete);
            
            _bizTalkHelper.UpdateSendPortUri("IndigoService", txtIndigoServiceServer.Text);
            
            _testStartTime = DateTime.Now;
            _avgCpuValue = 0;
            _avgProcessedValue = 0;
            _avgRreceivedValue = 0;
            _timerCount = 0;
            CPUGauge.MaxValue = 90;
            ProcessedGauge.MaxValue = 180;
            ReceivedGauge.MaxValue = 180;
            ProcessValue = 0;
            Progress.DataContext = this;
            //RunTest();
        }
        void OnStepComplete(object sender, StepEventArgs e)
        {
            switch (e.EventStep)
            { 
                case "UpdateSendPortUri":
                    picUpdateSendportUri.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_ok.png"));
                    picStartBizTalkArtefacts.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_run.png"));
                    DoEvents();
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() 
                        { 
                            _bizTalkHelper.CheckPortStatus("LoadGen_Receive", "LoadGen_Receive_NETTCP", "IndigoService", "EmptySchedule_Baseline1.SimpleSchedule");
                        }));
                    break;
                case "CheckPortStatus":
                    picStartBizTalkArtefacts.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_ok.png"));
                    picStartCollectorSets.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_run.png"));
                    DoEvents();
                    if (chbStartCollectorSets.IsChecked == true)
                    {
                        this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate()
                        {
                            _perflogHelper.StartCollectorSet();
                        }));
                    }
                    else
                        OnStepComplete(null, new StepEventArgs() { EventStep = "StartCollectorSet" });
                    break;
                case "StartCollectorSet":
                    picStartCollectorSets.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_ok.png"));
                    picInitPerfCounters.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_run.png"));
                    DoEvents();
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate()
                    {
                        _loadGenHelper.InitPerfCounters((Environment)environments.SelectedItem, (List<HostMaping>)lstHosts.DataContext, _bizTalkHelper.GetApplicationServerNames());
                    }));
                    break;
                case "InitPerfCounters":
                    picInitPerfCounters.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_ok.png"));
                    picStartLoadgen.Source = new BitmapImage(new Uri("pack://application:,,,/BizTalk Benchmark Wizard;component/Resources/Images/gear_run.png"));
                    DoEvents();
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate()
                    {
                        _loadGenHelper.StartLoadGenClients((Environment)environments.SelectedItem, (List<HostMaping>)lstHosts.DataContext);
                    }));
                    break;
                case "StartLoadGenClients":
                    lblTestTime.Text = string.Format("Total test duration: {0} minutes", ((int)_loadGenHelper.TestDuration / 60).ToString());
                    _timer = new System.Timers.Timer(TIMERTICKS);
                    _timer.Elapsed += new ElapsedEventHandler(OnCollectCounterData);
                    OnCollectCounterData(null, null);
                    _timer.Start();
                    tabControl1.SelectedIndex++;
                    btnNext.Visibility = Visibility.Visible;
                    btnNext.IsEnabled=false;
                    DoEvents();
                    break;
            }
        }
        private void OnTestComplete(object sender, LoadGen.LoadGenStopEventArgs e)
        {
            _perflogHelper.StopCollectorSet();
            
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { tabControl1.SelectedIndex++; }));
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { btnNext.IsEnabled = true; }));
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { _timer.Stop(); }));
        }
        private void OnCollectCounterData(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            
            float cpuValue=0;
            float processedValue=0;
            float receivedValue=0;

            try
            {
                foreach (PerfCounter c in _loadGenHelper.PerfCounters)
                {
                    cpuValue += (long)c.CPUCounterValue;

                    if (c.HasProcessingCounter)
                        processedValue += (long)c.ProcessedCounterValue;

                    if (c.HasReceiveCounter)
                        receivedValue += (long)c.ReceivedCounterValue;
                }
                double duration = DateTime.Now.Subtract(_testStartTime).TotalSeconds;

                
                if (duration > 120)
                {
                    if (_isWarmingUp)
                    {
                        _isWarmingUp = false;
                        this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { Progress.Foreground = Brushes.Green; }));
                    }
                    
                    _timerCount++;
                    _totalCpuValue += (long)cpuValue / _loadGenHelper.PerfCounters.Count;
                    _totalProcessedValue += (long)processedValue;
                    _totalRreceivedValue += (long)receivedValue;

                    _avgCpuValue = _totalCpuValue / _timerCount; //(long)((_avgCpuValue + cpuValue) * _timerCount);
                    _avgProcessedValue = _totalProcessedValue / _timerCount; //(long)((_avgProcessedValue + processedValue) * _timerCount);
                    _avgRreceivedValue = _totalRreceivedValue / _timerCount; //(long)((_avgRreceivedValue + receivedValue) * _timerCount);
                }

                // Set gauge values
                CPUGauge.SetCounter((int)cpuValue / _loadGenHelper.PerfCounters.Count, (int)_avgCpuValue);
                ProcessedGauge.SetCounter((int)processedValue, (int)_avgProcessedValue);
                ReceivedGauge.SetCounter((int)receivedValue, (int)_avgRreceivedValue);

                
                long percentCompleted = (long)((duration / _loadGenHelper.TestDuration) * 100);
                if(percentCompleted<=100)
                    this.Dispatcher.BeginInvoke(DispatcherPriority.DataBind , new Action(delegate(){ ProcessValue = (int)percentCompleted; }));
                DoEvents();
                
            }
            catch (Exception)
            {
                
                throw;
            }

            _timer.Enabled = true;
        }
        #endregion 
        #region IDisposable Members
        public void Dispose()
        {
            if (this._timer != null)
                this._timer.Dispose();

            if (this._bizTalkHelper != null)
                this._bizTalkHelper.Dispose();

            GC.SuppressFinalize(this);
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
        public string CounterName { get; set; }
        /// <summary>
        /// Test result
        /// </summary>
        public string TestValue { get; set; }
        /// <summary>
        /// Value collected from the Scenarios 
        /// </summary>
        public string Kpi { get; set; }
        /// <summary>
        /// Sucess / Failed
        /// </summary>
        public string Status { get; set; }
    }
    
    
}
