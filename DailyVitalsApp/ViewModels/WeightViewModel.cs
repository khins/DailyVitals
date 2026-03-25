using System;
using System.Collections.ObjectModel;
using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using DailyVitals.Domain.Models.Calculations;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class WeightViewModel : ViewModelBase
    {
        private readonly WeightService _service = new();
        private readonly PersonService _personService = new();

        private Person? _selectedPerson;
        private WeightReading? _selectedHistory;
        private string _weightValue = string.Empty;
        private string _heightFt = string.Empty;

        public bool IsEditMode => SelectedHistory != null;
        public bool CanUpdate => IsEditMode && CanSave;
        public bool CanSave => !string.IsNullOrWhiteSpace(WeightValue);

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<WeightReading> History { get; } = new();

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadHistory();
            }
        }

        public WeightReading? SelectedHistory
        {
            get => _selectedHistory;
            set
            {
                _selectedHistory = value;
                OnPropertyChanged();
                LoadFromHistory();
            }
        }

        public string WeightValue
        {
            get => _weightValue;
            set
            {
                _weightValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(BMICategory));
                OnPropertyChanged(nameof(BMIBrush));
                OnPropertyChanged(nameof(BMI));
            }
        }

        public string WeightUnit { get; set; } = "lb";
        public DateTime ReadingTime { get; set; } = DateTime.Now;
        public string? Notes { get; set; } = "Morning weigh-in";

        public string HeightFt
        {
            get => _heightFt;
            set
            {
                _heightFt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BMI));
                OnPropertyChanged(nameof(BMICategory));
                OnPropertyChanged(nameof(BMIBrush));
            }
        }

        public WeightViewModel()
        {
            LoadPersons();
            BeginNew();
        }

        public string BMICategory
        {
            get
            {
                if (BMI == null) return string.Empty;

                var bmi = BMI.Value;
                if (bmi < 18.5m) return "Underweight";
                if (bmi < 25.0m) return "Normal";
                if (bmi < 30.0m) return "Overweight";
                return "Obese";
            }
        }

        public Brush BMIBrush
        {
            get
            {
                if (BMI == null) return Brushes.Transparent;

                var bmi = BMI.Value;
                if (bmi < 18.5m) return Brushes.LightBlue;
                if (bmi < 25.0m) return Brushes.LightGreen;
                if (bmi < 30.0m) return Brushes.Khaki;
                return Brushes.IndianRed;
            }
        }

        public decimal? BMI
        {
            get
            {
                if (!decimal.TryParse(WeightValue, out var weight)) return null;
                if (!decimal.TryParse(HeightFt, out var height)) return null;
                return HealthMetrics.CalculateBMI(weight, height);
            }
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var person in _personService.GetAllPersons())
                Persons.Add(person);
        }

        private void LoadHistory()
        {
            History.Clear();
            if (SelectedPerson == null)
                return;

            foreach (var reading in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(reading);
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null)
                return;

            WeightValue = SelectedHistory.WeightValue.ToString("0.##");
            WeightUnit = SelectedHistory.WeightUnit;
            ReadingTime = SelectedHistory.ReadingTime;
            Notes = SelectedHistory.Notes;
            HeightFt = SelectedHistory.HeightFt?.ToString("0.##") ?? string.Empty;

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
            HeightFt = string.Empty;

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

            if (IsEditMode)
            {
                var selectedHistory = SelectedHistory
                    ?? throw new InvalidOperationException("No weight entry selected.");

                _service.UpdateWeight(
                    selectedHistory.WeightId,
                    weight,
                    WeightUnit,
                    ReadingTime,
                    Notes ?? string.Empty,
                    Environment.UserName);
            }
            else
            {
                _service.InsertWeight(
                    SelectedPerson.PersonId,
                    weight,
                    WeightUnit,
                    ReadingTime,
                    Notes ?? string.Empty,
                    Environment.UserName);
            }

            LoadHistory();
            BeginNew();
        }

        public void DeleteSelected()
        {
            if (SelectedHistory == null)
                return;

            _service.DeleteWeight(
                SelectedHistory.WeightId,
                Environment.UserName);

            LoadHistory();
            BeginNew();
        }
    }
}
