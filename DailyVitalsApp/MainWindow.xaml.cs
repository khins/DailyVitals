using DailyVitals.App.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DailyVitals.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenBloodPressure_Click(object sender, RoutedEventArgs e)
        {
            var bpView = new BloodPressureView();
            bpView.Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenBloodGlucose_Click(object sender, RoutedEventArgs e)
        {
            var window = new BloodGlucoseView
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private void OpenWeight_Click(object sender, RoutedEventArgs e)
        {
            var window = new WeightView { Owner = this };
            window.ShowDialog();
        }

        private void OpenDashboard_Click(object sender, RoutedEventArgs e)
        {
            var window = new VitalsDashboardView { Owner = this };
            window.ShowDialog();
        }

        private void OpenMedicationEntry_Click(object sender, RoutedEventArgs e)
        {
            var window = new MedicationEntryView { Owner = this };
            window.ShowDialog();
        }

        private void OpenMedicationList_Click(object sender, RoutedEventArgs e)
        {
            var window = new MedicationListView
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void OpenExercise_Click(object sender, RoutedEventArgs e)
        {
            new ExerciseEntryView { Owner = this }.ShowDialog();
        }
    }
}