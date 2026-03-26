using DailyVitals.App.ViewModels;
using System.Windows;

namespace DailyVitals.App.Views
{
    /// <summary>
    /// Interaction logic for ExerciseTrendView.xaml
    /// </summary>
    public partial class ExerciseTrendView : Window
    {
        public ExerciseTrendView(long personId, string personName)
        {
            InitializeComponent();
            DataContext = new ExerciseTrendViewModel(personId, personName);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
