using DailyVitals.Data.Services;
using DailyVitals.Data.Services.DailyVitals.App.Services;
using DailyVitals.Domain.Models;
using DailyVitals.Domain.Models.Calculations;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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
                LoadSelectedSession();
            }
        }

        public bool IsEditMode { get; private set; }
        public long? EditingExerciseSessionId { get; private set; }
        private DateTime? _createdAt;
        private DateTime? _updatedAt;

        private string _durationMinutes = string.Empty;
        public string DurationMinutes
        {
            get => _durationMinutes;
            set
            {
                _durationMinutes = value;
                OnPropertyChanged();
            }
        }

        private string _caloriesExpended = string.Empty;
        public string CaloriesExpended
        {
            get => _caloriesExpended;
            set
            {
                _caloriesExpended = value;
                OnPropertyChanged();
            }
        }

        private DateTime _startTime = DateTime.Today;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value.Date;
                OnPropertyChanged();
            }
        }

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

            LoadSelectedSession();
        }

        public void BeginNew()
        {
            SelectedSession = null;
            ClearEntry();
        }

        private void LoadSelectedSession()
        {
            if (SelectedSession == null)
                return;

            SelectedExercise = ExerciseTypes.FirstOrDefault(exerciseType =>
                exerciseType.ExerciseTypeId == SelectedSession.ExerciseTypeId);
            DurationMinutes = SelectedSession.DurationMinutes.ToString();
            CaloriesExpended = SelectedSession.CaloriesExpended?.ToString() ?? string.Empty;
            SelectedIntensity = SelectedSession.Intensity;
            Notes = SelectedSession.Notes ?? string.Empty;
            StartTime = SelectedSession.StartTime;
            EditingExerciseSessionId = SelectedSession.ExerciseSessionId;
            _createdAt = SelectedSession.CreatedAt;
            _updatedAt = SelectedSession.UpdatedAt;
            IsEditMode = true;
            OnPropertyChanged(nameof(AuditTimestampText));
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

            var editingExerciseSessionId = EditingExerciseSessionId;
            if (editingExerciseSessionId.HasValue)
            {
                _service.UpdateExerciseSession(
                    editingExerciseSessionId.Value,
                    SelectedPerson.PersonId,
                    SelectedExercise.ExerciseTypeId,
                    StartTime,
                    durationMinutes,
                    caloriesExpended,
                    SelectedIntensity,
                    Notes,
                    Environment.UserName);
            }
            else
            {
                _service.InsertExerciseSession(
                    SelectedPerson.PersonId,
                    SelectedExercise.ExerciseTypeId,
                    StartTime,
                    durationMinutes,
                    caloriesExpended,
                    SelectedIntensity,
                    Notes,
                    Environment.UserName);
            }

            LoadHistory();
            if (editingExerciseSessionId.HasValue)
                SelectedSession = History.FirstOrDefault(session => session.ExerciseSessionId == editingExerciseSessionId.Value);
            else
                ClearEntry();
        }

        private void ClearEntry()
        {
            DurationMinutes = string.Empty;
            CaloriesExpended = string.Empty;
            Notes = string.Empty;
            SelectedIntensity = "Moderate";
            StartTime = DateTime.Today;
            EditingExerciseSessionId = null;
            IsEditMode = false;
            _createdAt = null;
            _updatedAt = null;
            OnPropertyChanged(nameof(AuditTimestampText));
        }
    }
}
