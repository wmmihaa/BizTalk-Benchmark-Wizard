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
using System.Windows.Threading;
using System.Threading;

namespace bLogical.GaugeControl
{
    /// <summary>
    /// Interaction logic for GaugeControl.xaml
    /// </summary>
    public partial class GaugeControl : UserControl
    {
        const int MINVALUE = 30;
        const int MAXVALUE = 300;
        int _maxValue = MAXVALUE;
        public int MaxValue
        {
            get { return _maxValue; }
            set
            {
                if (l2 != null)
                {
                    int stepVal = value / 9;
                    l2.Text = stepVal.ToString();
                    l3.Text = (stepVal * 2).ToString();
                    l4.Text = (stepVal * 3).ToString();
                    l5.Text = (stepVal * 4).ToString();
                    l6.Text = (stepVal * 5).ToString();
                    l7.Text = (stepVal * 6).ToString();
                    l8.Text = (stepVal * 7).ToString();
                    l9.Text = (stepVal * 8).ToString();
                    l10.Text = (stepVal * 9).ToString();
                }
                _maxValue = value;
                DoEvents();
            }
        }
        public string Caption { get; set; }

        public GaugeControl()
        {
            this.MaxValue = 300;
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            lblDescription.Text = Caption;
            needle.DataContext = this;
            avgNeedle.DataContext = this;
            SetCounter(0, 0);
        }
        public static readonly DependencyProperty CounterProperty = DependencyProperty.Register(
                                                                            "Counter",
                                                                            typeof(int),
                                                                            typeof(GaugeControl),
                                                                            new PropertyMetadata(0));
        public static readonly DependencyProperty AvgCounterProperty = DependencyProperty.Register(
                                                                            "AvgCounter",
                                                                            typeof(int),
                                                                            typeof(GaugeControl),
                                                                            new PropertyMetadata(0));
        /// <summary>
        /// Gets or sets the Counter property.
        /// </summary>
        public int Counter
        {
            set
            {
                int newValue = value;

                if (newValue > MaxValue + MINVALUE)
                    newValue = MaxValue + MINVALUE;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate { SetValue(CounterProperty, newValue); },
                    value);
            }
            get
            {
                int counter = (int)this.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    (DispatcherOperationCallback)delegate { return GetValue(CounterProperty); }, CounterProperty);

                if (counter > MaxValue + MINVALUE)
                    return MaxValue + MINVALUE;

                return counter;
            }
        }
        /// <summary>
        /// Gets or sets the Average Counter property.
        /// </summary>
        public int AvgCounter
        {
            set
            {
                int newValue = value;

                if (newValue > MaxValue + MINVALUE)
                    newValue = MaxValue + MINVALUE;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate { SetValue(AvgCounterProperty, newValue); },
                    value);

            }
            get
            {
                int counter = (int)this.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    (DispatcherOperationCallback)delegate { return GetValue(CounterProperty); }, CounterProperty);

                if (counter > MaxValue + MINVALUE)
                    return MaxValue + MINVALUE;

                return counter;
            }
        }
        public void SetCounter(int newCounterValue, int newAvgCounterValue)
        {
            if (newCounterValue > MaxValue)
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(delegate() { DoubleMaxValue(); }));
                
            newCounterValue = ConvertValue(newCounterValue);
            newAvgCounterValue = ConvertValue(newAvgCounterValue);

            bool up = newCounterValue > Counter;
            int start = Counter;
            int internalCounter = start;
            while (internalCounter != newCounterValue)
            {
                Dictionary<GaugeControl, int> tst = new Dictionary<GaugeControl, int>();
                tst.Add(this, internalCounter);
                ThreadPool.QueueUserWorkItem(new WaitCallback(rotateNeedleStatic), tst);
                internalCounter = up ? internalCounter + 1 : internalCounter - 1;
            }
            AvgCounter = newAvgCounterValue;
        }
        int ConvertValue(int val)
        {
            double percentage = (double)MAXVALUE / (double)MaxValue;
            double newVal = val * percentage;
            return (int)newVal + MINVALUE;
        }
        public void SetCounter_OLD(int newCounterValue, int newAvgCounterValue)
        {
            bool up = newCounterValue > Counter;
            int start = Counter;
            int internalCounter = start;
            while (internalCounter != newCounterValue)
            {
                Dictionary<GaugeControl, int> tst = new Dictionary<GaugeControl, int>();
                tst.Add(this, internalCounter);
                ThreadPool.QueueUserWorkItem(new WaitCallback(rotateNeedleStatic), tst);
                internalCounter = up ? internalCounter + 1 : internalCounter - 1;
            }
            AvgCounter = newAvgCounterValue;
        }
        private static void rotateNeedleStatic(Object info)
        {
            GaugeControl tst = ((Dictionary<GaugeControl, int>)info).Keys.Single<GaugeControl>();
            int n = ((Dictionary<GaugeControl, int>)info).Values.Single<int>();
            tst.Counter = n;
            Thread.Sleep(10);

        }
        private void DoubleMaxValue()
        {
            MaxValue = MaxValue * 2;
        }
        private static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
        }
    }
}
