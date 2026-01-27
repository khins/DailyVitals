using DailyVitals.Data.Services;
using DailyVitals.Data.Services.DailyVitals.App.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;

namespace DailyVitals.App.ViewModels
{
    public class ExerciseEntryViewModel : ViewModelBase
    {
        private readonly ExerciseService _service = new();
        private readonly PersonService _personService = new();

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<ExerciseType> ExerciseTypes { get; } = new();
        public ObservableCollection<ExerciseSession> History { get; } = new();

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

        private ExerciseType _selectedExercise;
        public ExerciseType SelectedExercise
        {
            get => _selectedExercise;
            set
            {
                _selectedExercise = value;
                OnPropertyChanged();
            }
        }

        private ExerciseSession _selectedSession;
        public ExerciseSession SelectedSession
        {
            get => _selectedSession;
            set
            {
                _selectedSession = value;
                OnPropertyChanged();
            }
        }
        //public ExerciseType? SelectedExerciseType { get; set; }

        public bool IsEditMode { get; private set; }
        public long? EditingExerciseSessionId { get; private set; }


        public string DurationMinutes { get; set; } = "";
        public string Intensity { get; set; } = "Moderate";
        public DateTime StartTime { get; set; } = DateTime.Now;
        private string _notes;
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
            foreach (var p in _personService.GetPeople())
                Persons.Add(p);

            foreach (var t in _service.GetExerciseTypes())
                ExerciseTypes.Add(t);
        }

        public void LoadHistory()
        {
            History.Clear();

            if (SelectedPerson == null)
                return;

            foreach (var h in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(h);
        }

        public void BeginEdit()
        {
            if (SelectedSession == null)
                return;

            //SelectedExercise = ExerciseTypes.FirstOrDefault(
            //    x => x.ExerciseTypeId == SelectedSession.ExerciseTypeId);

            DurationMinutes = SelectedSession.DurationMinutes.ToString();
            SelectedIntensity = SelectedSession.Intensity;
            Notes = SelectedSession.Notes;

            OnPropertyChanged(nameof(DurationMinutes));
            OnPropertyChanged(nameof(Intensity));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(StartTime));
        }

        public void DeleteSelected()
        {
            if (SelectedExercise == null)
                return;

            _service.DeleteExerciseSession(
                SelectedSession.ExerciseSessionId,
                Environment.UserName);

            LoadHistory();
        }

        public ObservableCollection<string> Intensities { get; } =
                                   new() { "Low", "Moderate", "High" };

        private string _selectedIntensity;
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

            if (!int.TryParse(DurationMinutes, out var minutes) || minutes <= 0)
                throw new InvalidOperationException("Duration must be a positive number.");

            _service.InsertExerciseSession(
                SelectedPerson.PersonId,
                SelectedExercise.ExerciseTypeId,
                StartTime,
                minutes,
                Intensity,
                Notes,
                Environment.UserName
            );

            LoadHistory();
            ClearEntry();
        }

        private void ClearEntry()
        {
            DurationMinutes = "";
            Notes = "";
            StartTime = DateTime.Now;

            OnPropertyChanged(nameof(DurationMinutes));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(StartTime));
        }
    }

}
