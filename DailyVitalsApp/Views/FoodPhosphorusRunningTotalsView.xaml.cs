using DailyVitals.App.ViewModels;
using System.Windows;

namespace DailyVitals.App.Views
{
    public partial class FoodPhosphorusRunningTotalsView : Window
    {
        public FoodPhosphorusRunningTotalsView(long personId, string personName)
        {
            InitializeComponent();
            DataContext = new FoodPhosphorusRunningTotalsViewModel(personId, personName);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
