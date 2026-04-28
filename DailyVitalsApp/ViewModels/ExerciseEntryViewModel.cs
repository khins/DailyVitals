using DailyVitals.Data.Services;
using DailyVitals.Data.Services.DailyVitals.App.Services;
using DailyVitals.Domain.Models;
using DailyVitals.Domain.Models.Calculations;
using System;
using System.Collections.ObjectModel;

namespace DailyVitals.App.ViewModels
{
    public class ExerciseEntryViewModel : ViewModelBase
    {
        private readonly ExerciseService _service = new();
        private readonly PersonService _personService = new();
        private readonly WeightService _weightService = new();

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<ExerciseType> ExerciseTypes { get; } = new();
        public ObservableCollection<ExerciseSession> History { get; } = new();

        private Person? _selectedPerson;
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

        private ExerciseType? _selectedExercise;
        public ExerciseType? SelectedExercise
        {
            get => _selectedExercise;
            set
            {
                _selectedExercise = value;
                OnPropertyChanged();
            }
        }

        private ExerciseSession? _selectedSession;
        public ExerciseSession? SelectedSession
        {
            get => _selectedSession;
            set
            {
                _selectedSession = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditMode { get; private set; }
        public long? EditingExerciseSessionId { get; private set; }

        public string DurationMinutes { get; set; } = string.Empty;
        public string CaloriesExpended { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }

        public ExerciseEntryViewModel()
        {
            foreach (var person in _personService.GetPeople())
                Persons.Add(person);

            foreach (var exerciseType in _service.GetExerciseTypes())
                ExerciseTypes.Add(exerciseType);
        }

        public void LoadHistory()
        {
            History.Clear();

            if (SelectedPerson == null)
                return;

            foreach (var session in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(session);
        }

        public void BeginEdit()
        {
            if (SelectedSession == null)
                return;

            DurationMinutes = SelectedSession.DurationMinutes.ToString();
            CaloriesExpended = SelectedSession.CaloriesExpended?.ToString() ?? string.Empty;
            SelectedIntensity = SelectedSession.Intensity;
            Notes = SelectedSession.Notes ?? string.Empty;
            StartTime = SelectedSession.StartTime;

            OnPropertyChanged(nameof(DurationMinutes));
            OnPropertyChanged(nameof(CaloriesExpended));
            OnPropertyChanged(nameof(SelectedIntensity));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(StartTime));
        }

        public void DeleteSelected()
        {
            if (SelectedSession == null)
                return;

            _service.DeleteExerciseSession(
                SelectedSession.ExerciseSessionId,
                Environment.UserName);

            LoadHistory();
        }

        public ObservableCollection<string> Intensities { get; } =
            new() { "Low", "Moderate", "High" };

        private string _selectedIntensity = "Moderate";
        public string SelectedIntensity
        {
            get => _selectedIntensity;
            set
            {
                _selectedIntensity = value;
                OnPropertyChanged();
            }
        }

        public void Save()
        {
            if (SelectedPerson == null || SelectedExercise == null)
                throw new InvalidOperationException("Select person and exercise type.");

            if (!decimal.TryParse(DurationMinutes, out var durationMinutes))
                throw new InvalidOperationException("Duration must be a valid number.");

            if (durationMinutes <= 0)
                throw new InvalidOperationException("Duration must be greater than zero.");

            decimal? caloriesExpended = null;
            if (!string.IsNullOrWhiteSpace(CaloriesExpended))
            {
                if (!decimal.TryParse(CaloriesExpended, out var parsedCalories))
                    throw new InvalidOperationException("Calories expended must be a valid number.");

                if (parsedCalories < 0)
                    throw new InvalidOperationException("Calories expended cannot be negative.");

                caloriesExpended = parsedCalories;
            }
            else
            {
                var latestWeight = _weightService.GetLatestForPerson(SelectedPerson.PersonId);
                if (latestWeight != null)
                {
                    caloriesExpended = ExerciseMetrics.EstimateCaloriesBurned(
                        durationMinutes,
                        SelectedIntensity,
                        latestWeight.WeightValue,
                        latestWeight.WeightUnit);
                }
            }

            _service.InsertExerciseSession(
                SelectedPerson.PersonId,
                SelectedExercise.ExerciseTypeId,
                StartTime,
                durationMinutes,
                caloriesExpended,
                SelectedIntensity,
                Notes,
                Environment.UserName
            );

            LoadHistory();
            ClearEntry();
        }

        private void ClearEntry()
        {
            DurationMinutes = string.Empty;
            CaloriesExpended = string.Empty;
            Notes = string.Empty;
            SelectedIntensity = "Moderate";
            StartTime = DateTime.Now;

            OnPropertyChanged(nameof(DurationMinutes));
            OnPropertyChanged(nameof(CaloriesExpended));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(SelectedIntensity));
            OnPropertyChanged(nameof(StartTime));
        }
    }
}
