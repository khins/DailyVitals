using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;


namespace DailyVitals.App.ViewModels
{
    public class BloodPressureViewModel : INotifyPropertyChanged
    {
        private readonly PersonService _personService;
        private readonly BloodPressureService _bpService;

        private long? _currentBpId;
        public bool IsEditMode => _currentBpId.HasValue;


        public ObservableCollection<Person> Persons { get; }

        private Person _selectedPerson;
        public Person SelectedPerson
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

        private string _systolic;
        public string Systolic
        {
            get => _systolic;
            set
            {
                _systolic = value;
                OnPropertyChanged(nameof(Systolic));
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        private string _diastolic;
        public string Diastolic
        {
            get => _diastolic;
            set
            {
                _diastolic = value;
                OnPropertyChanged(nameof(Diastolic));
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(CanSave));
            }
        }
        public string Pulse { get; set; }
        public DateTime ReadingTime { get; set; } = DateTime.Now;
        public string Notes { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action RequestClose;

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

            _personService = new PersonService();
            _bpService = new BloodPressureService();

            Notes = "Morning reading, seated";

            LoadPersons();

            Systolic = string.Empty;
            Diastolic = string.Empty;
            Pulse = string.Empty;

        }

        public bool CanSave
        {
            get =>
                !string.IsNullOrWhiteSpace(Systolic) &&
                !string.IsNullOrWhiteSpace(Diastolic);
        }


        private void LoadPersons()
        {
            foreach (var p in _personService.GetAllPersons())
                Persons.Add(p);
        }

        public long Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Please select a person.");

            var systolic = Convert.ToInt32(Systolic);
            var diastolic = Convert.ToInt32(Diastolic);
            var pulse = Convert.ToInt32(Pulse);

            if (systolic <= diastolic)
                throw new InvalidOperationException("Systolic must be greater than diastolic.");


            if (IsEditMode)
            {
                _bpService.UpdateBloodPressure(
                    _currentBpId.Value,
                    systolic,
                    diastolic,
                    pulse,
                    ReadingTime,
                    Notes,
                    Environment.UserName
                );

                return _currentBpId.Value;
            }
            else
            {
                return _bpService.InsertBloodPressure(
                    SelectedPerson.PersonId,
                    systolic,
                    diastolic,
                    pulse,
                    ReadingTime,
                    Notes,
                    Environment.UserName
                );
            }
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
        }

        public void ClearFields()
        {
            _currentBpId = null;
            Systolic = string.Empty;
            Diastolic = string.Empty;
            Pulse = string.Empty;
            Notes = "Morning reading, seated";
            ReadingTime = DateTime.Now;
        }

        public ObservableCollection<BloodPressureReading> History { get; }
                 = new ObservableCollection<BloodPressureReading>();

        private BloodPressureReading _selectedHistory;
        public BloodPressureReading SelectedHistory
        {
            get => _selectedHistory;
            set
            {
                _selectedHistory = value;
                OnPropertyChanged(nameof(SelectedHistory));
                LoadFromHistory();
            }
        }

        private void LoadHistory()
        {
            History.Clear();

            if (SelectedPerson == null)
                return;

            var records = _bpService.GetHistoryForPerson(SelectedPerson.PersonId);
            foreach (var r in records)
                History.Add(r);
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

            OnPropertyChanged(nameof(ReadingTime));
            OnPropertyChanged(nameof(Pulse));
        }


        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
