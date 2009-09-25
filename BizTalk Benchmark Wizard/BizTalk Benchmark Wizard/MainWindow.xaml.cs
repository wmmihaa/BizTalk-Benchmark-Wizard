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

namespace BizTalk_Benchmark_Wizard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _isLogedIn = false;
        List<Scenario> _scenarios;
        public IEnumerable<Environment> Environments;
        public List<Result> Results = new List<Result>();
        public MainWindow()
        {
            InitializeComponent();
            
            _scenarios = ScenariosFactory.Load();
            

            foreach (var scenario in _scenarios)
                cbScenario.Items.Add(scenario.Name);

            cbScenario.SelectedIndex = 0;

            Environments = _scenarios[0].Environments;
    
            this.environments.DataContext = Environments;
        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl1.SelectedIndex > 0)
                btnBack.Visibility = Visibility.Visible;
            else
                btnBack.Visibility = Visibility.Hidden;

            if (tabControl1.SelectedIndex > 5)
                btnNext.Visibility = Visibility.Hidden;
            else
                btnNext.Visibility = Visibility.Visible;

            tabControl1.SelectedIndex--;
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl1.SelectedIndex == 0 && !_isLogedIn)
            {
                PopupLogin.IsOpen = true;
                return;
            }

            if (tabControl1.SelectedIndex > 0)
                btnBack.Visibility = Visibility.Visible;
            else
                btnBack.Visibility = Visibility.Hidden;

            if (tabControl1.SelectedIndex == 5)
            {
                btnNext.Visibility = Visibility.Hidden;
                ShowResult();
            }
            else
                btnNext.Visibility = Visibility.Visible;

            tabControl1.SelectedIndex++;

        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            PopupLogin.IsOpen = false;
            this.Cursor = Cursors.Wait;
            try
            {
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
        private void btnExceptionOk_Click(object sender, RoutedEventArgs e)
        {
            PopupException.IsOpen = false;
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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        void ShowResult()
        {
            Results.Add(new Result() { CouterValue = "Avg Processor time (SQL)", TestValue = "50", KPI = "< 60", Status = "Succeeded" });
            Results.Add(new Result() { CouterValue = "Avg Processor time (BizTalk)", TestValue = "89", KPI = "< 90", Status = "Succeeded" });
            Results.Add(new Result() { CouterValue = "Avg Processed msgs / sec (*)", TestValue = "344", KPI = "> 500", Status = "Failed" });
            ResultGrid.DataContext = Results;
        }
    }
    public class Result
    {
        public string CouterValue { get; set; }
        public string TestValue { get; set; }
        public string KPI { get; set; }
        public string Status { get; set; }
    }
}
