using DailyVitals.App.ViewModels;
using System;
using System.Windows;

namespace DailyVitals.App.Views
{
    public partial class PersonEntryView : Window
    {
        private readonly PersonEntryViewModel _vm;

        public PersonEntryView()
        {
            InitializeComponent();
            _vm = new PersonEntryViewModel();
            DataContext = _vm;
        }

        private void New_Click(object sender, RoutedEventArgs e)
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
