using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyVitals.App.ViewModels
{
    public class FoodPhosphorusIntakeViewModel : ViewModelBase
    {
        private readonly FoodPhosphorusIntakeService _service = new();
        private readonly FoodPhosphorusEstimateService _estimateService = new();
        private readonly PersonService _personService = new();

        private string _foodName = string.Empty;
        private string _phosphorusMg = string.Empty;
        private string? _notes;
        private string? _servingDescription;
        private string? _aiConfidence;
        private string? _sourceNotes;
        private bool _estimatedByAi;
        private bool _isEstimating;
        private string _estimateStatus = string.Empty;
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
        public bool CanEstimate => !IsEstimating && !string.IsNullOrWhiteSpace(FoodName);

        public string FoodName
        {
            get => _foodName;
            set
            {
                _foodName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanEstimate));
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

        public string? ServingDescription
        {
            get => _servingDescription;
            set
            {
                _servingDescription = value;
                OnPropertyChanged();
            }
        }

        public string? AiConfidence
        {
            get => _aiConfidence;
            private set
            {
                _aiConfidence = value;
                OnPropertyChanged();
            }
        }

        public string? SourceNotes
        {
            get => _sourceNotes;
            private set
            {
                _sourceNotes = value;
                OnPropertyChanged();
            }
        }

        public bool EstimatedByAi
        {
            get => _estimatedByAi;
            private set
            {
                _estimatedByAi = value;
                OnPropertyChanged();
            }
        }

        public bool IsEstimating
        {
            get => _isEstimating;
            private set
            {
                _isEstimating = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEstimate));
            }
        }

        public string EstimateStatus
        {
            get => _estimateStatus;
            private set
            {
                _estimateStatus = value;
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
            ServingDescription = SelectedHistory.ServingDescription;
            EstimatedByAi = SelectedHistory.EstimatedByAi;
            AiConfidence = SelectedHistory.AiConfidence;
            SourceNotes = SelectedHistory.SourceNotes;
            EstimateStatus = EstimatedByAi
                ? $"AI estimate: {AiConfidence ?? "unknown confidence"}"
                : string.Empty;
        }

        public void BeginNew()
        {
            SelectedHistory = null;
            FoodName = string.Empty;
            PhosphorusMg = string.Empty;
            ConsumedAt = DateTime.Today;
            Notes = string.Empty;
            ServingDescription = string.Empty;
            EstimatedByAi = false;
            AiConfidence = null;
            SourceNotes = null;
            EstimateStatus = string.Empty;
        }

        public async System.Threading.Tasks.Task EstimatePhosphorusAsync()
        {
            IsEstimating = true;
            EstimateStatus = "Estimating phosphorus...";

            try
            {
                var estimate = await _estimateService.EstimateAsync(FoodName);

                FoodName = estimate.FoodName;
                PhosphorusMg = estimate.EstimatedPhosphorusMg.ToString();
                ServingDescription = estimate.ServingDescription;
                EstimatedByAi = true;
                AiConfidence = estimate.Confidence;
                SourceNotes = estimate.SourceNotes;
                Notes = BuildAiNotes(estimate);
                EstimateStatus = $"Estimate ready: {estimate.EstimatedPhosphorusMg} mg ({estimate.Confidence ?? "unknown confidence"})";
            }
            finally
            {
                IsEstimating = false;
            }
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
                ServingDescription,
                EstimatedByAi,
                EstimatedByAi ? "Gemini" : null,
                AiConfidence,
                SourceNotes,
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

        private static string BuildAiNotes(FoodPhosphorusEstimate estimate)
        {
            var serving = string.IsNullOrWhiteSpace(estimate.ServingDescription)
                ? null
                : $"Serving: {estimate.ServingDescription}";
            var confidence = string.IsNullOrWhiteSpace(estimate.Confidence)
                ? null
                : $"Confidence: {estimate.Confidence}";
            var sourceNotes = string.IsNullOrWhiteSpace(estimate.SourceNotes)
                ? null
                : estimate.SourceNotes;

            return string.Join(Environment.NewLine, new[] { serving, confidence, sourceNotes }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }
}
