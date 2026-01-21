using DailyVitals.App.ViewModels;
using System.Windows;

namespace DailyVitals.App.Views
{
    /// <summary>
    /// Interaction logic for BloodPressureView.xaml
    /// </summary>
    public partial class BloodPressureView : Window
    {
        private readonly BloodPressureViewModel _vm;
        public BloodPressureView()
        {
            InitializeComponent();
            _vm = new BloodPressureViewModel();

            DataContext = _vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var bpId = _vm.Save();

                MessageBox.Show(
                    $"Blood pressure saved successfully (ID {bpId}).",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
