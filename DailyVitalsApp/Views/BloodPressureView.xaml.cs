using DailyVitals.App.ViewModels;
using System.Windows;

namespace DailyVitals.App.Views
{
    /// <summary>
    /// Interaction logic for BloodPressureView.xaml
    /// </summary>
    public partial class BloodPressureView : Window
    {
        public BloodPressureView()
        {
            InitializeComponent();
            DataContext = new BloodPressureViewModel();
        }
    }
}
