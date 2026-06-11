using DailyVitals.App.ViewModels;
using System.Windows;

namespace DailyVitals.App.Views
{
    public partial class NutritionAnalyticsView : Window
    {
        public NutritionAnalyticsView()
        {
            InitializeComponent();
            DataContext = new NutritionAnalyticsViewModel();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
