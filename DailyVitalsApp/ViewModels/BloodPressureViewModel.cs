using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DailyVitals.Domain.Models;
using DailyVitals.Data.Services;


namespace DailyVitals.App.ViewModels
{
    public class BloodPressureViewModel : INotifyPropertyChanged
    {
        private readonly PersonService _personService;
        private readonly BloodPressureService _bpService;

        public ObservableCollection<Person> Persons { get; }

        private Person _selectedPerson;
        public Person SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged(nameof(SelectedPerson));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _systolic;
        public string Systolic
        {
            get => _systolic;
            set
            {
                _systolic = value;
                OnPropertyChanged(nameof(Systolic));
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        private string _diastolic;
        public string Diastolic
        {
            get => _diastolic;
            set
            {
                _diastolic = value;
                OnPropertyChanged(nameof(Diastolic));
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(CanSave));
            }
        }
        public string Pulse { get; set; }
        public DateTime ReadingTime { get; set; } = DateTime.Now;
        public string Notes { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action RequestClose;

        public BloodPressureViewModel()
        {
            Persons = new ObservableCollection<Person>();

            _personService = new PersonService();
            _bpService = new BloodPressureService();

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Persons.Add(new Person { PersonId = 1, FirstName = "John", LastName = "Doe" });
                SelectedPerson = Persons[0];
                return;
            }

            _personService = new PersonService();
            _bpService = new BloodPressureService();

            Notes = "Morning reading, seated";

            LoadPersons();

            Systolic = string.Empty;
            Diastolic = string.Empty;
            Pulse = string.Empty;

        }

        public bool CanSave
        {
            get =>
                !string.IsNullOrWhiteSpace(Systolic) &&
                !string.IsNullOrWhiteSpace(Diastolic);
        }


        private void LoadPersons()
        {
            foreach (var p in _personService.GetAllPersons())
                Persons.Add(p);
        }

        public long Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Please select a person.");

            var systolic = Convert.ToInt32(Systolic);
            var diastolic = Convert.ToInt32(Diastolic);
            var pulse = Convert.ToInt32(Pulse);

            if (systolic <= diastolic)
                throw new InvalidOperationException("Systolic must be greater than diastolic.");

            return _bpService.InsertBloodPressure(
                SelectedPerson.PersonId,
                Convert.ToInt32(systolic),
                Convert.ToInt32(diastolic),
                Convert.ToInt32(pulse),
                ReadingTime,
                Notes,
                Environment.UserName
            );
        }



        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
