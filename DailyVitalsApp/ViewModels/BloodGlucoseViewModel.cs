using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;

namespace DailyVitals.App.ViewModels
{
    public class BloodGlucoseViewModel : ViewModelBase
    {
        private readonly BloodGlucoseService _service = new();
        private readonly PersonService _personService = new();

        private string _glucose = string.Empty;
        private Person? _selectedPerson;
        private BloodGlucoseReading? _selectedHistory;

        public BloodGlucoseViewModel()
        {
            LoadPersons();
            BeginNew();
        }

        public ObservableCollection<BloodGlucoseReading> History { get; } = new();
        public ObservableCollection<Person> Persons { get; } = new();

        public bool CanDelete => SelectedHistory != null;
        public bool CanSave => !string.IsNullOrWhiteSpace(Glucose);

        public string Glucose
        {
            get => _glucose;
            set
            {
                _glucose = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadHistoryForSelectedPerson();
            }
        }

        public bool Fasting { get; set; }
        public DateTime ReadingTime { get; set; } = DateTime.Now;
        public string? Notes { get; set; } = "Morning reading";

        public BloodGlucoseReading? SelectedHistory
        {
            get => _selectedHistory;
            set
            {
                _selectedHistory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDelete));
                LoadFromHistory();
            }
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var person in _personService.GetAllPersons())
                Persons.Add(person);
        }

        private void LoadHistoryForSelectedPerson()
        {
            History.Clear();

            if (SelectedPerson == null)
                return;

            foreach (var reading in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(reading);
        }

        public void LoadHistory(long personId)
        {
            History.Clear();
            foreach (var reading in _service.GetHistory(personId))
                History.Add(reading);
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null)
                return;

            Glucose = SelectedHistory.GlucoseValue.ToString();
            ReadingTime = SelectedHistory.ReadingTime;
            Notes = SelectedHistory.Notes;
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
        }

        public void BeginNew()
        {
            SelectedHistory = null;
            Glucose = string.Empty;
            Fasting = false;
            ReadingTime = DateTime.Now;
            Notes = "Morning reading";
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
        }

        public void Save(long personId)
        {
            if (!int.TryParse(Glucose, out var value))
                throw new InvalidOperationException("Invalid glucose value.");

            _service.Insert(
                personId,
                value,
                ReadingTime,
                Notes ?? string.Empty,
                Environment.UserName);
        }

        public void DeleteSelected()
        {
            if (SelectedHistory == null)
                return;

            _service.DeleteBloodGlucose(
                SelectedHistory.GlucoseId,
                Environment.UserName);

            LoadHistoryForSelectedPerson();
            BeginNew();
        }
    }
}
