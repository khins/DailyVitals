using DailyVitals.App.ViewModels;
using System;
using System.Windows;

namespace DailyVitals.App.Views
{
    public partial class KidneyLabResultsView : Window
    {
        private readonly KidneyLabResultsViewModel _vm;

        public KidneyLabResultsView()
        {
            InitializeComponent();
            _vm = new KidneyLabResultsViewModel();
            DataContext = _vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.Save();
                MessageBox.Show("Monthly kidney labs saved.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void New_Click(object sender, RoutedEventArgs e) => _vm.BeginNew();

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.CanDelete)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this monthly kidney lab result?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _vm.DeleteSelected();
                MessageBox.Show("Monthly kidney lab result deleted.", "Deleted");
            }
        }
    }
}
