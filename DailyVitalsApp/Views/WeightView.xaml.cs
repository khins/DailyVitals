using System;
using System.Windows;
using DailyVitals.App.ViewModels;

namespace DailyVitals.App.Views
{
    public partial class WeightView : Window
    {
        private readonly WeightViewModel _vm;

        public WeightView()
        {
            InitializeComponent();
            _vm = new WeightViewModel();
            DataContext = _vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.Save();
                MessageBox.Show("Weight saved.", "Success");
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
            if (_vm.SelectedHistory == null)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this weight entry?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _vm.DeleteSelected();
                MessageBox.Show("Weight entry deleted.", "Deleted");
            }
        }

    }
}
