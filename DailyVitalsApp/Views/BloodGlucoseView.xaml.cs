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
    /// Interaction logic for BloodGlucoseView.xaml
    /// </summary>
    public partial class BloodGlucoseView : Window
    {
        private readonly BloodGlucoseViewModel _vm;
        public BloodGlucoseView()
        {
            InitializeComponent();

            _vm = new BloodGlucoseViewModel();
            DataContext = _vm;
        }

        private void NewReading_Click(object sender, RoutedEventArgs e)
        {
            _vm.BeginNew();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_vm.SelectedPerson == null)
                {
                    MessageBox.Show(
                        "Please select a person.",
                        "Missing Person",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _vm.Save(_vm.SelectedPerson.PersonId);

                MessageBox.Show(
                    "Blood glucose reading saved successfully.",
                    "Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Prepare for next entry
                _vm.BeginNew();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.CanDelete)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this blood glucose reading?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _vm.DeleteSelected();

                MessageBox.Show(
                    "Blood glucose reading deleted.",
                    "Deleted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

    }
}
