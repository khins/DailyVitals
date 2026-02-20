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
    /// Interaction logic for ExerciseEntryView.xaml
    /// </summary>
    public partial class ExerciseEntryView : Window
    {
        private readonly ExerciseEntryViewModel _vm = new();

        public ExerciseEntryView()
        {
            InitializeComponent();
            DataContext = _vm;
        }

        private void Person_Changed(object sender, SelectionChangedEventArgs e)
        {
            _vm.LoadHistory();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _vm.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            _vm.BeginEdit();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedExercise == null)
                return;

            var result = MessageBox.Show(
                "Are you sure you want to permanently delete this exercise session?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _vm.DeleteSelected();
            }
        }

        private void Duration_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string proposed = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            e.Handled = !decimal.TryParse(proposed, out _);
        }
    }
}
