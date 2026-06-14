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
        private DateTime? _birthDate;
        private string _gender = string.Empty;
        private DateTime? _createdAt;
        private DateTime? _updatedAt;

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
                OnPropertyChanged(nameof(CanDelete));
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

        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                _birthDate = value;
                OnPropertyChanged();
            }
        }

        public string Gender
        {
            get => _gender;
            set
            {
                _gender = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMale));
                OnPropertyChanged(nameof(IsFemale));
            }
        }

        public bool IsMale
        {
            get => string.Equals(Gender, "Male", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                    Gender = "Male";
            }
        }

        public bool IsFemale
        {
            get => string.Equals(Gender, "Female", StringComparison.OrdinalIgnoreCase);
            set
            {
                if (value)
                    Gender = "Female";
            }
        }

        public string AuditTimestampText
        {
            get
            {
                if (_updatedAt.HasValue)
                    return $"Updated at: {_updatedAt.Value:g}";

                if (_createdAt.HasValue)
                    return $"Created at: {_createdAt.Value:g}";

                return string.Empty;
            }
        }

        public bool CanSave =>
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName) &&
            IsOptionalPositiveDecimal(HeightFt);
        public bool CanDelete => SelectedPerson != null;

        public void BeginNew()
        {
            SelectedPerson = null;
            FirstName = string.Empty;
            LastName = string.Empty;
            HeightFt = string.Empty;
            BirthDate = null;
            Gender = string.Empty;
            _createdAt = null;
            _updatedAt = null;
            OnPropertyChanged(nameof(AuditTimestampText));
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
                    heightFt,
                    BirthDate,
                    Gender.Trim());
            }
            else
            {
                _personService.UpdatePerson(
                    SelectedPerson.PersonId,
                    FirstName.Trim(),
                    LastName.Trim(),
                    heightFt,
                    BirthDate,
                    Gender.Trim());
            }

            LoadPeople();
            SelectedPerson = People.FirstOrDefault(person => person.PersonId == selectedPersonId);
        }

        public void DeleteSelected()
        {
            if (SelectedPerson == null)
                return;

            _personService.DeletePerson(SelectedPerson.PersonId);
            LoadPeople();
            BeginNew();
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
            BirthDate = SelectedPerson.BirthDate;
            Gender = SelectedPerson.Gender ?? string.Empty;
            _createdAt = SelectedPerson.CreatedAt;
            _updatedAt = SelectedPerson.UpdatedAt;
            OnPropertyChanged(nameof(AuditTimestampText));
        }

        private static bool IsOptionalPositiveDecimal(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                (decimal.TryParse(value, out var parsedValue) && parsedValue > 0);
        }
    }
}
