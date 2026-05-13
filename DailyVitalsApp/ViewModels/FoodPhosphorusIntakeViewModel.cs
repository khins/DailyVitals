using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;

namespace DailyVitals.App.ViewModels
{
    public class FoodPhosphorusIntakeViewModel : ViewModelBase
    {
        private readonly FoodPhosphorusIntakeService _service = new();
        private readonly PersonService _personService = new();

        private string _foodName = string.Empty;
        private string _phosphorusMg = string.Empty;
        private string? _notes;
        private DateTime _consumedAt = DateTime.Today;
        private Person? _selectedPerson;
        private FoodPhosphorusIntake? _selectedHistory;
        private int _selectedDayTotalMg;

        public FoodPhosphorusIntakeViewModel()
        {
            LoadPersons();
            BeginNew();
        }

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<FoodPhosphorusIntake> History { get; } = new();

        public bool CanSave =>
            SelectedPerson != null &&
            !string.IsNullOrWhiteSpace(FoodName) &&
            int.TryParse(PhosphorusMg, out var phosphorus) &&
            phosphorus >= 0;

        public bool CanDelete => SelectedHistory != null;

        public string FoodName
        {
            get => _foodName;
            set
            {
                _foodName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string PhosphorusMg
        {
            get => _phosphorusMg;
            set
            {
                _phosphorusMg = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public DateTime ConsumedAt
        {
            get => _consumedAt;
            set
            {
                _consumedAt = value;
                OnPropertyChanged();
                RefreshSelectedDayTotal();
            }
        }

        public string? Notes
        {
            get => _notes;
            set
            {
                _notes = value;
                OnPropertyChanged();
            }
        }

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                LoadHistoryForSelectedPerson();
                RefreshSelectedDayTotal();
            }
        }

        public FoodPhosphorusIntake? SelectedHistory
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

        public int SelectedDayTotalMg
        {
            get => _selectedDayTotalMg;
            private set
            {
                _selectedDayTotalMg = value;
                OnPropertyChanged();
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

            foreach (var item in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(item);
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null)
                return;

            FoodName = SelectedHistory.FoodName;
            PhosphorusMg = SelectedHistory.PhosphorusMg.ToString();
            ConsumedAt = SelectedHistory.ConsumedAt;
            Notes = SelectedHistory.Notes;
        }

        public void BeginNew()
        {
            SelectedHistory = null;
            FoodName = string.Empty;
            PhosphorusMg = string.Empty;
            ConsumedAt = DateTime.Today;
            Notes = string.Empty;
        }

        public void Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Please select a person.");

            if (string.IsNullOrWhiteSpace(FoodName))
                throw new InvalidOperationException("Food item is required.");

            if (!int.TryParse(PhosphorusMg, out var phosphorus) || phosphorus < 0)
                throw new InvalidOperationException("Phosphorus must be a non-negative whole number.");

            _service.Insert(
                SelectedPerson.PersonId,
                FoodName.Trim(),
                phosphorus,
                ConsumedAt,
                Notes,
                Environment.UserName);

            LoadHistoryForSelectedPerson();
            RefreshSelectedDayTotal();
        }

        public void DeleteSelected()
        {
            if (SelectedHistory == null)
                return;

            _service.Delete(
                SelectedHistory.FoodPhosphorusIntakeId,
                Environment.UserName);

            LoadHistoryForSelectedPerson();
            BeginNew();
            RefreshSelectedDayTotal();
        }

        private void RefreshSelectedDayTotal()
        {
            SelectedDayTotalMg = SelectedPerson == null
                ? 0
                : _service.GetDailyTotal(SelectedPerson.PersonId, ConsumedAt);
        }
    }
}
