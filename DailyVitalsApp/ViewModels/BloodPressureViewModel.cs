using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class BloodPressureViewModel : INotifyPropertyChanged
    {
        private readonly PersonService _personService;
        private readonly BloodPressureService _bpService;
        private long? _currentBpId;
        private Person? _selectedPerson;
        private string _systolic = string.Empty;
        private string _diastolic = string.Empty;
        private BloodPressureReading? _selectedHistory;

        public BloodPressureViewModel()
        {
            Persons = new ObservableCollection<Person>();
            _personService = new PersonService();
            _bpService = new BloodPressureService();

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Persons.Add(new Person { PersonId = 1, FirstName = "John", LastName = "Doe" });
                SelectedPerson = Persons[0];
                return;
            }

            Notes = "Morning reading, seated";
            Pulse = string.Empty;

            LoadPersons();
            Systolic = string.Empty;
            Diastolic = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsEditMode => _currentBpId.HasValue;
        public bool CanSave => !string.IsNullOrWhiteSpace(Systolic) && !string.IsNullOrWhiteSpace(Diastolic);
        public bool CanDelete => SelectedHistory != null;
        public ObservableCollection<Person> Persons { get; }
        public ObservableCollection<BloodPressureReading> History { get; } = new();

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged(nameof(SelectedPerson));
                LoadLatestBloodPressure();
                LoadHistory();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Systolic
        {
            get => _systolic;
            set
            {
                _systolic = value;
                OnPropertyChanged(nameof(Systolic));
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(SeverityText));
                OnPropertyChanged(nameof(SeverityBrush));
            }
        }

        public string Diastolic
        {
            get => _diastolic;
            set
            {
                _diastolic = value;
                OnPropertyChanged(nameof(Diastolic));
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(SeverityText));
                OnPropertyChanged(nameof(SeverityBrush));
            }
        }

        public string Pulse { get; set; } = string.Empty;
        public DateTime ReadingTime { get; set; } = DateTime.Now;
        public string? Notes { get; set; } = "Morning reading, seated";

        public BloodPressureReading? SelectedHistory
        {
            get => _selectedHistory;
            set
            {
                _selectedHistory = value;
                OnPropertyChanged(nameof(SelectedHistory));
                OnPropertyChanged(nameof(CanDelete));
                LoadFromHistory();
            }
        }

        public long Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Please select a person.");

            var systolic = Convert.ToInt32(Systolic);
            var diastolic = Convert.ToInt32(Diastolic);
            var pulse = 0;

            if (!string.IsNullOrWhiteSpace(Pulse) && !int.TryParse(Pulse, out pulse))
                throw new InvalidOperationException("Invalid pulse value.");

            if (systolic <= diastolic)
                throw new InvalidOperationException("Systolic must be greater than diastolic.");

            if (IsEditMode)
            {
                var currentBpId = _currentBpId
                    ?? throw new InvalidOperationException("No blood pressure entry selected for update.");

                _bpService.UpdateBloodPressure(
                    currentBpId,
                    systolic,
                    diastolic,
                    pulse,
                    ReadingTime,
                    Notes ?? string.Empty,
                    Environment.UserName);

                return currentBpId;
            }

            return _bpService.InsertBloodPressure(
                SelectedPerson.PersonId,
                systolic,
                diastolic,
                pulse,
                ReadingTime,
                Notes ?? string.Empty,
                Environment.UserName);
        }

        private void LoadPersons()
        {
            foreach (var person in _personService.GetAllPersons())
                Persons.Add(person);
        }

        private void LoadLatestBloodPressure()
        {
            ClearFields();

            if (SelectedPerson == null)
                return;

            var bp = _bpService.GetLatestForPerson(SelectedPerson.PersonId);
            if (bp == null)
                return;

            _currentBpId = bp.BloodPressureId;
            Systolic = bp.Systolic.ToString();
            Diastolic = bp.Diastolic.ToString();
            Pulse = bp.Pulse.ToString();
            ReadingTime = bp.ReadingTime;
            Notes = bp.Notes;
            OnPropertyChanged(nameof(Pulse));
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
        }

        public void ClearFields()
        {
            _currentBpId = null;
            Systolic = string.Empty;
            Diastolic = string.Empty;
            Pulse = string.Empty;
            Notes = "Morning reading, seated";
            ReadingTime = DateTime.Now;
            OnPropertyChanged(nameof(Pulse));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(ReadingTime));
        }

        private void LoadHistory()
        {
            History.Clear();

            if (SelectedPerson == null)
                return;

            var records = _bpService.GetHistoryForPerson(SelectedPerson.PersonId);
            foreach (var record in records)
                History.Add(record);

            OnPropertyChanged(nameof(SeverityText));
            OnPropertyChanged(nameof(SeverityBrush));
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null)
                return;

            _currentBpId = SelectedHistory.BloodPressureId;
            Systolic = SelectedHistory.Systolic.ToString();
            Diastolic = SelectedHistory.Diastolic.ToString();
            Pulse = SelectedHistory.Pulse.ToString();
            ReadingTime = SelectedHistory.ReadingTime;
            Notes = SelectedHistory.Notes;

            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Pulse));
        }

        public string SeverityText
        {
            get
            {
                if (!int.TryParse(Systolic, out var systolic) ||
                    !int.TryParse(Diastolic, out var diastolic))
                    return string.Empty;

                if (systolic < 120 && diastolic < 80)
                    return "Normal";

                if (systolic < 130 && diastolic < 80)
                    return "Elevated";

                return "High";
            }
        }

        public Brush SeverityBrush =>
            SeverityText switch
            {
                "Normal" => Brushes.LightGreen,
                "Elevated" => Brushes.Gold,
                "High" => Brushes.IndianRed,
                _ => Brushes.Transparent
            };

        public void BeginNewReading()
        {
            _currentBpId = null;
            SelectedHistory = null;
            Systolic = string.Empty;
            Diastolic = string.Empty;
            Pulse = string.Empty;
            ReadingTime = DateTime.Now;
            Notes = "Morning reading, seated";

            OnPropertyChanged(nameof(SeverityText));
            OnPropertyChanged(nameof(SeverityBrush));
            OnPropertyChanged(nameof(Pulse));
            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Notes));
        }

        public void DeleteSelected()
        {
            if (SelectedHistory == null)
                return;

            _bpService.DeleteBloodPressure(SelectedHistory.BloodPressureId);
            LoadHistory();
            BeginNewReading();
        }

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
