using System.Windows;

namespace DailyVitals.App.Views
{
    public partial class FoodPhosphorusFoodNotesView : Window
    {
        public FoodPhosphorusFoodNotesView(string foodName, string noteText)
        {
            InitializeComponent();
            FoodNameText.Text = foodName;
            NotesTextBox.Text = noteText;
        }

        public string NoteText { get; private set; } = string.Empty;

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            NoteText = NotesTextBox.Text;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
