using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;


namespace DailyVitals.App.ViewModels
{
    public class BloodGlucoseViewModel : ViewModelBase
    {
        private readonly BloodGlucoseService _service = new();
        private readonly PersonService _personService = new();

        public BloodGlucoseViewModel()
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

        private void LoadHistoryForSelectedPerson()
        {
            History.Clear();

            if (SelectedPerson == null)
                return;

            foreach (var r in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(r);
        }


        public ObservableCollection<BloodGlucoseReading> History { get; }
            = new();

        private string _glucose;
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

        public ObservableCollection<Person> Persons { get; }
               = new ObservableCollection<Person>();


        private Person _selectedPerson;
        public Person SelectedPerson
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
        public string Notes { get; set; } = "Morning reading";

        private BloodGlucoseReading _selectedHistory;
        public BloodGlucoseReading SelectedHistory
        {
            get => _selectedHistory;
            set
            {
                _selectedHistory = value;
                OnPropertyChanged(nameof(SelectedHistory));
                LoadFromHistory();
            }
        }


        public bool CanSave => !string.IsNullOrWhiteSpace(Glucose);

        public void LoadHistory(long personId)
        {
            History.Clear();
            foreach (var r in _service.GetHistory(personId))
                History.Add(r);
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null) return;

            Glucose = SelectedHistory.GlucoseValue.ToString();
            ReadingTime = SelectedHistory.ReadingTime;
            Notes = SelectedHistory.Notes;
        }

        public void BeginNew()
        {
            SelectedHistory = null;
            Glucose = string.Empty;
            Fasting = false;
            ReadingTime = DateTime.Now;
            Notes = "Morning reading";
        }

        public void Save(long personId)
        {
            if (!int.TryParse(Glucose, out var value))
                throw new InvalidOperationException("Invalid glucose value.");

            _service.Insert(
                personId,
                value,
                ReadingTime,
                Notes,
                Environment.UserName);
        }
    }

}
