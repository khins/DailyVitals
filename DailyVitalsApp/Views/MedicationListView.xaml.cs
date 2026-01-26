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
using static DailyVitals.App.ViewModels.MedicationListViewModel;

namespace DailyVitals.App.Views
{
    /// <summary>
    /// Interaction logic for MedicationListView.xaml
    /// </summary>
    public partial class MedicationListView : Window
    {
        public MedicationListView()
        {
            InitializeComponent();
            DataContext = new MedicationListViewModel();
        }

        private void Deactivate_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Deactivate this medication?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            ((MedicationListViewModel)DataContext).DeactivateSelected();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MedicationListViewModel)DataContext;

            if (vm.SelectedMedication == null)
                return;

            var window = new MedicationEntryView(vm.SelectedMedication)
            {
                Owner = this
            };

            window.ShowDialog();
            vm.Refresh();
        }

    }

}
