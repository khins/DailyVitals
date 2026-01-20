using System.Collections.ObjectModel;
using System.ComponentModel;
using DailyVitals.Domain.Models;
using DailyVitals.Data.Services;

namespace DailyVitals.App.ViewModels
{
    public class BloodPressureViewModel : INotifyPropertyChanged
    {
        private readonly PersonService _personService;

        public ObservableCollection<Person> Persons { get; }
        private Person _selectedPerson;

        public Person SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged(nameof(SelectedPerson));
                OnPropertyChanged(nameof(SelectedPersonId));
            }
        }

        public long SelectedPersonId => SelectedPerson?.PersonId ?? 0;

        public BloodPressureViewModel()
        {
            _personService = new PersonService();
            Persons = new ObservableCollection<Person>();

            LoadPersons();
        }

        private void LoadPersons()
        {
            var people = _personService.GetAllPersons();
            foreach (var person in people)
                Persons.Add(person);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
