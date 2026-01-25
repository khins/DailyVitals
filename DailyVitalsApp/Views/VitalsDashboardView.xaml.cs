using DailyVitals.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DailyVitals.App.Views
{
    /// <summary>
    /// Interaction logic for VitalsDashboardView.xaml
    /// </summary>
    public partial class VitalsDashboardView : Window
    {
        public VitalsDashboardView()
        {
            InitializeComponent();
            DataContext = new VitalsDashboardViewModel();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
