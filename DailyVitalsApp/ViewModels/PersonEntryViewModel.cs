using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyVitals.App.ViewModels
{
    public class PersonEntryViewModel : ViewModelBase
    {
        private readonly PersonService _personService = new();
        private Person? _selectedPerson;
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _heightFt = string.Empty;

        public PersonEntryViewModel()
        {
            LoadPeople();
            BeginNew();
        }

        public ObservableCollection<Person> People { get; } = new();

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                LoadFromSelected();
            }
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string HeightFt
        {
            get => _heightFt;
            set
            {
                _heightFt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool CanSave =>
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName) &&
            IsOptionalPositiveDecimal(HeightFt);

        public void BeginNew()
        {
            SelectedPerson = null;
            FirstName = string.Empty;
            LastName = string.Empty;
            HeightFt = string.Empty;
        }

        public void Save()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new InvalidOperationException("First name is required.");

            if (string.IsNullOrWhiteSpace(LastName))
                throw new InvalidOperationException("Last name is required.");

            decimal? heightFt = null;
            if (!string.IsNullOrWhiteSpace(HeightFt))
            {
                if (!decimal.TryParse(HeightFt, out var parsedHeightFt) || parsedHeightFt <= 0)
                    throw new InvalidOperationException("Height must be a positive number in feet.");

                heightFt = parsedHeightFt;
            }

            var selectedPersonId = SelectedPerson?.PersonId;
            if (SelectedPerson == null)
            {
                selectedPersonId = _personService.InsertPerson(
                    FirstName.Trim(),
                    LastName.Trim(),
                    heightFt);
            }
            else
            {
                _personService.UpdatePerson(
                    SelectedPerson.PersonId,
                    FirstName.Trim(),
                    LastName.Trim(),
                    heightFt);
            }

            LoadPeople();
            SelectedPerson = People.FirstOrDefault(person => person.PersonId == selectedPersonId);
        }

        private void LoadPeople()
        {
            People.Clear();
            foreach (var person in _personService.GetAllPersons())
                People.Add(person);
        }

        private void LoadFromSelected()
        {
            if (SelectedPerson == null)
                return;

            FirstName = SelectedPerson.FirstName;
            LastName = SelectedPerson.LastName;
            HeightFt = SelectedPerson.HeightFt?.ToString("0.##") ?? string.Empty;
        }

        private static bool IsOptionalPositiveDecimal(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                (decimal.TryParse(value, out var parsedValue) && parsedValue > 0);
        }
    }
}
