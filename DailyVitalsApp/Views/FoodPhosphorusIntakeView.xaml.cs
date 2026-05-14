using DailyVitals.App.ViewModels;
using System;
using System.Windows;

namespace DailyVitals.App.Views
{
    public partial class FoodPhosphorusIntakeView : Window
    {
        private readonly FoodPhosphorusIntakeViewModel _vm;

        public FoodPhosphorusIntakeView()
        {
            InitializeComponent();

            _vm = new FoodPhosphorusIntakeViewModel();
            DataContext = _vm;
        }

        private void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            _vm.BeginNew();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.Save();

                MessageBox.Show(
                    "Food phosphorus entry saved successfully.",
                    "Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

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

        private async void Estimate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _vm.EstimatePhosphorusAsync();

                MessageBox.Show(
                    "Phosphorus estimate added to the form. Review the details before saving.",
                    "Estimate Ready",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Estimate Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.CanDelete)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this food phosphorus entry?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _vm.DeleteSelected();

            MessageBox.Show(
                "Food phosphorus entry deleted.",
                "Deleted",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
