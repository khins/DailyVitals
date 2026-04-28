using DailyVitals.App.ViewModels;
using System.Windows;

namespace DailyVitals.App.Views
{
    public partial class KidneyLabTrendView : Window
    {
        public KidneyLabTrendView(long personId, string personName)
        {
            InitializeComponent();
            DataContext = new KidneyLabTrendViewModel(personId, personName);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
