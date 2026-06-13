using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyVitals.App.ViewModels
{
    public class BloodGlucoseViewModel : ViewModelBase
    {
        private readonly BloodGlucoseService _service = new();
        private readonly PersonService _personService = new();

        private string _glucose = string.Empty;
        private Person? _selectedPerson;
        private BloodGlucoseReading? _selectedHistory;
        private DateTime? _createdAt;
        private DateTime? _updatedAt;

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
            _createdAt = SelectedHistory.CreatedAt;
            _updatedAt = SelectedHistory.UpdatedAt;
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(AuditTimestampText));
        }

        public void BeginNew()
        {
            SelectedHistory = null;
            Glucose = string.Empty;
            Fasting = false;
            ReadingTime = DateTime.Now;
            Notes = "Morning reading";
            _createdAt = null;
            _updatedAt = null;
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(AuditTimestampText));
        }

        public void Save(long personId)
        {
            if (!int.TryParse(Glucose, out var value))
                throw new InvalidOperationException("Invalid glucose value.");

            var savedGlucoseId = SelectedHistory?.GlucoseId;

            if (savedGlucoseId.HasValue)
            {
                _service.Update(
                    savedGlucoseId.Value,
                    personId,
                    value,
                    ReadingTime,
                    Notes ?? string.Empty,
                    Environment.UserName);
            }
            else
            {
                savedGlucoseId = _service.Insert(
                    personId,
                    value,
                    ReadingTime,
                    Notes ?? string.Empty,
                    Environment.UserName);
            }

            LoadHistoryForSelectedPerson();
            SelectedHistory = History.FirstOrDefault(reading => reading.GlucoseId == savedGlucoseId.Value);
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
