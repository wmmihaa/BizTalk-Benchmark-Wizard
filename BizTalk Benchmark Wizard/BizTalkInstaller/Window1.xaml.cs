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

namespace BizTalkInstaller
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
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
        }
        
    }
}
