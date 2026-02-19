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

        private void WeightCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not VitalsDashboardViewModel vm ||
                vm.SelectedPerson == null)
                return;

            var window = new WeightTrendView(
                vm.SelectedPerson.PersonId,
                vm.SelectedPerson.FullName)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void BloodPressureCard_Click(object sender, MouseButtonEventArgs e)
        {
            var vm = (VitalsDashboardViewModel)DataContext;

            if (vm.SelectedPerson == null)
                return;

            var window = new BloodPressureTrendView(
                vm.SelectedPerson.PersonId,
                vm.SelectedPerson.FullName)
            {
                Owner = this
            };

            window.ShowDialog();
        }

    }
}
