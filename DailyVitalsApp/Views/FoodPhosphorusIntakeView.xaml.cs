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

        private void RunningTotals_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedPerson == null)
            {
                MessageBox.Show(
                    "Please select a person before viewing running totals.",
                    "Person Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var report = new FoodPhosphorusRunningTotalsView(
                _vm.SelectedPerson.PersonId,
                _vm.SelectedPerson.FullName)
            {
                Owner = this
            };

            report.ShowDialog();
        }

        private void FoodNotes_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.CanEditFoodNotes || _vm.SelectedHistory == null)
            {
                MessageBox.Show(
                    "Save or select a food entry before editing food notes.",
                    "Food Entry Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var notesWindow = new FoodPhosphorusFoodNotesView(
                _vm.SelectedHistory.FoodName,
                _vm.LoadFoodNote())
            {
                Owner = this
            };

            if (notesWindow.ShowDialog() != true)
                return;

            try
            {
                _vm.SaveFoodNote(notesWindow.NoteText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Food Notes Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
