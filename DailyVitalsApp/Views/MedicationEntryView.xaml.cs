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
    /// Interaction logic for MedicationEntryView.xaml
    /// </summary>
    public partial class MedicationEntryView : Window
    {
        public MedicationEntryView()
        {
            InitializeComponent();
            DataContext = new MedicationViewModel();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((MedicationViewModel)DataContext).Save();
                MessageBox.Show("Medication saved.");
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
    }

}
