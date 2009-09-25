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

namespace BizTalk_Benchmark_Wizard.Resources.Styles
{
    /// <summary>
    /// Interaction logic for Gauge.xaml
    /// </summary>
    public partial class Gauge : UserControl
    {
        public string Caption { get; set; }
        public Gauge()
        {
            InitializeComponent();
           
        }
        public static readonly DependencyProperty CounterProperty = DependencyProperty.Register(
                                                                            "Counter",
                                                                            typeof(int),
                                                                            typeof(Gauge),
                                                                            new PropertyMetadata(0));
        /// <summary>
        /// Gets or sets the Counter property.
        /// </summary>
        public int Counter
        {
            set
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate { SetValue(CounterProperty, value); },
                    value);
            }
            get
            {

                return (int)this.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    (DispatcherOperationCallback)delegate { return GetValue(CounterProperty); }, CounterProperty);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            lblDescription.Text= Caption;
            //Counter = 30;
        }
    }
}
