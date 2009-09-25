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

        public string Caption { get; set; }

        public GaugeControl()
        {
            InitializeComponent();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            lblDescription.Text = Caption;
            needle.DataContext = this;
            DigitalGauge.DataContext = this;
            avgNeedle.DataContext = this;
            //Counter = 30;
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
                int newValue = value / Level;

                if (newValue > MAXVALUE + MINVALUE)
                    newValue = MAXVALUE + MINVALUE;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate { SetValue(CounterProperty, newValue); },
                    value);

            }
            get
            {
                int counter = (int)this.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    (DispatcherOperationCallback)delegate { return GetValue(CounterProperty); }, CounterProperty);

                if (counter * Level > MAXVALUE + MINVALUE)
                    return MAXVALUE + MINVALUE;

                return counter * Level;
            }
        }
        /// <summary>
        /// Gets or sets the Average Counter property.
        /// </summary>
        public int AvgCounter
        {
            set
            {
                int newValue = value / Level;

                if (newValue > MAXVALUE + MINVALUE)
                    newValue = MAXVALUE + MINVALUE;

                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate { SetValue(AvgCounterProperty, newValue); },
                    value);

            }
            get
            {
                int counter = (int)this.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    (DispatcherOperationCallback)delegate { return GetValue(CounterProperty); }, CounterProperty);

                if (counter * Level > MAXVALUE + MINVALUE)
                    return MAXVALUE + MINVALUE;

                return counter * Level;
            }
        }
 
        public int Level { get; set; }

        public void SetCounter (int newCounterValue, int newAvgCounterValue)
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
    }
}
