using System;
using System.Collections.ObjectModel;
using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;

namespace DailyVitals.App.ViewModels
{
    public class WeightViewModel : ViewModelBase
    {
        private readonly WeightService _service = new();
        private readonly PersonService _personService = new();

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<WeightReading> History { get; } = new();

        private Person _selectedPerson;
        public Person SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadHistory();
            }
        }

        private WeightReading _selectedHistory;
        public WeightReading SelectedHistory
        {
            get => _selectedHistory;
            set
            {
                _selectedHistory = value;
                OnPropertyChanged();
                LoadFromHistory();
            }
        }

        public string WeightValue { get; set; }
        public string WeightUnit { get; set; } = "lb";
        public DateTime ReadingTime { get; set; } = DateTime.Now;
        public string Notes { get; set; } = "Morning weigh-in";

        public bool CanSave => !string.IsNullOrWhiteSpace(WeightValue);

        public WeightViewModel()
        {
            LoadPersons();
            BeginNew();
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var p in _personService.GetAllPersons())
                Persons.Add(p);
        }

        private void LoadHistory()
        {
            History.Clear();
            if (SelectedPerson == null) return;

            foreach (var r in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(r);
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null) return;

            WeightValue = SelectedHistory.WeightValue.ToString("0.##");
            WeightUnit = SelectedHistory.WeightUnit;
            ReadingTime = SelectedHistory.ReadingTime;
            Notes = SelectedHistory.Notes;

            OnPropertyChanged(nameof(WeightValue));
            OnPropertyChanged(nameof(WeightUnit));
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
        }

        public void BeginNew()
        {
            SelectedHistory = null;
            WeightValue = string.Empty;
            WeightUnit = "lb";
            ReadingTime = DateTime.Now;
            Notes = "Morning weigh-in";

            OnPropertyChanged(nameof(WeightValue));
            OnPropertyChanged(nameof(WeightUnit));
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
        }

        public void Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Please select a person.");

            if (!decimal.TryParse(WeightValue, out var weight))
                throw new InvalidOperationException("Invalid weight value.");

            _service.InsertWeight(
                SelectedPerson.PersonId,
                weight,
                WeightUnit,
                ReadingTime,
                Notes,
                Environment.UserName);

            LoadHistory();
            BeginNew();
        }
    }

}
