using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
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
        private string _calories = string.Empty;
        private string _sodiumMg = string.Empty;
        private string _proteinG = string.Empty;
        private string _potassiumMg = string.Empty;
        private string _binders = "0";
        private string _searchText = string.Empty;
        private string? _notes;
        private string? _foodNotes;
        private string? _servingDescription;
        private string? _aiConfidence;
        private string? _sourceNotes;
        private bool _estimatedByAi;
        private bool _isEstimating;
        private string _estimateStatus = string.Empty;
        private DateTime _consumedAt = DateTime.Today;
        private DateTime? _createdAt;
        private DateTime? _updatedAt;
        private Person? _selectedPerson;
        private FoodPhosphorusIntake? _selectedHistory;
        private long? _editingFoodPhosphorusIntakeId;
        private DateTime? _editingConsumedDate;
        private int? _editingFluidMl;
        private int _selectedDayTotalMg;
        private readonly List<FoodPhosphorusIntake> _allHistory = new();

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
            phosphorus >= 0 &&
            IsOptionalNonNegativeWholeNumber(Calories) &&
            IsOptionalNonNegativeWholeNumber(SodiumMg) &&
            IsOptionalNonNegativeDecimal(ProteinG) &&
            IsOptionalNonNegativeWholeNumber(PotassiumMg) &&
            int.TryParse(Binders, out var binders) &&
            binders >= 0;

        public bool CanDelete => SelectedHistory != null;
        public bool CanEstimate => !IsEstimating && !string.IsNullOrWhiteSpace(FoodName);
        public bool CanViewRunningTotals => SelectedPerson != null;
        public bool CanEditFoodNotes => SelectedHistory != null;
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

        public string Binders
        {
            get => _binders;
            set
            {
                _binders = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string Calories
        {
            get => _calories;
            set
            {
                _calories = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string SodiumMg
        {
            get => _sodiumMg;
            set
            {
                _sodiumMg = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string ProteinG
        {
            get => _proteinG;
            set
            {
                _proteinG = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string PotassiumMg
        {
            get => _potassiumMg;
            set
            {
                _potassiumMg = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;
                OnPropertyChanged();
                ApplyHistorySearch();
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

        public string? FoodNotes
        {
            get => _foodNotes;
            set
            {
                _foodNotes = value;
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
                OnPropertyChanged(nameof(CanViewRunningTotals));
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
                OnPropertyChanged(nameof(CanEditFoodNotes));
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

            if (SelectedPerson == null && Persons.Count > 0)
                SelectedPerson = Persons[0];
        }

        private void LoadHistoryForSelectedPerson()
        {
            History.Clear();
            _allHistory.Clear();

            if (SelectedPerson == null)
                return;

            foreach (var item in _service.GetHistory(SelectedPerson.PersonId))
                _allHistory.Add(item);

            ApplyHistorySearch();
        }

        private void ApplyHistorySearch()
        {
            History.Clear();

            var searchText = SearchText.Trim();
            var filteredHistory = string.IsNullOrWhiteSpace(searchText)
                ? _allHistory
                : _allHistory
                    .Where(item =>
                        item.FoodName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(item.ServingDescription) &&
                            item.ServingDescription.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(item.Notes) &&
                            item.Notes.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(item.FoodNotes) &&
                            item.FoodNotes.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            foreach (var item in filteredHistory)
                History.Add(item);
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null)
                return;

            FoodName = SelectedHistory.FoodName;
            PhosphorusMg = SelectedHistory.PhosphorusMg.ToString();
            Calories = SelectedHistory.Calories?.ToString() ?? string.Empty;
            SodiumMg = SelectedHistory.SodiumMg?.ToString() ?? string.Empty;
            ProteinG = SelectedHistory.ProteinG?.ToString("0.##") ?? string.Empty;
            PotassiumMg = SelectedHistory.PotassiumMg?.ToString() ?? string.Empty;
            Binders = SelectedHistory.Binders.ToString();
            ConsumedAt = SelectedHistory.ConsumedAt;
            Notes = SelectedHistory.Notes;
            FoodNotes = SelectedHistory.FoodNotes;
            ServingDescription = SelectedHistory.ServingDescription;
            EstimatedByAi = SelectedHistory.EstimatedByAi;
            AiConfidence = SelectedHistory.AiConfidence;
            SourceNotes = SelectedHistory.SourceNotes;
            _createdAt = SelectedHistory.CreatedAt;
            _updatedAt = SelectedHistory.UpdatedAt;
            _editingFoodPhosphorusIntakeId = SelectedHistory.FoodPhosphorusIntakeId;
            _editingConsumedDate = SelectedHistory.ConsumedAt.Date;
            _editingFluidMl = SelectedHistory.FluidMl;
            EstimateStatus = EstimatedByAi
                ? $"AI estimate: {AiConfidence ?? "unknown confidence"}"
                : string.Empty;
            OnPropertyChanged(nameof(AuditTimestampText));
        }

        public void BeginNew()
        {
            SelectedHistory = null;
            FoodName = string.Empty;
            PhosphorusMg = string.Empty;
            Calories = string.Empty;
            SodiumMg = string.Empty;
            ProteinG = string.Empty;
            PotassiumMg = string.Empty;
            Binders = "0";
            ConsumedAt = DateTime.Today;
            Notes = string.Empty;
            FoodNotes = string.Empty;
            ServingDescription = string.Empty;
            EstimatedByAi = false;
            AiConfidence = null;
            SourceNotes = null;
            EstimateStatus = string.Empty;
            _createdAt = null;
            _updatedAt = null;
            _editingFoodPhosphorusIntakeId = null;
            _editingConsumedDate = null;
            _editingFluidMl = null;
            OnPropertyChanged(nameof(AuditTimestampText));
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
                FoodNotes = BuildAiNotes(estimate);
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

            int? calories = null;
            if (!string.IsNullOrWhiteSpace(Calories))
            {
                if (!int.TryParse(Calories, out var parsedCalories) || parsedCalories < 0)
                    throw new InvalidOperationException("Calories must be a non-negative whole number.");

                calories = parsedCalories;
            }

            int? sodiumMg = null;
            if (!string.IsNullOrWhiteSpace(SodiumMg))
            {
                if (!int.TryParse(SodiumMg, out var parsedSodiumMg) || parsedSodiumMg < 0)
                    throw new InvalidOperationException("Sodium must be a non-negative whole number in mg.");

                sodiumMg = parsedSodiumMg;
            }

            decimal? proteinG = null;
            if (!string.IsNullOrWhiteSpace(ProteinG))
            {
                if (!decimal.TryParse(ProteinG, out var parsedProteinG) || parsedProteinG < 0)
                    throw new InvalidOperationException("Protein must be a non-negative number in grams.");

                proteinG = parsedProteinG;
            }

            int? potassiumMg = null;
            if (!string.IsNullOrWhiteSpace(PotassiumMg))
            {
                if (!int.TryParse(PotassiumMg, out var parsedPotassiumMg) || parsedPotassiumMg < 0)
                    throw new InvalidOperationException("Potassium must be a non-negative whole number in mg.");

                potassiumMg = parsedPotassiumMg;
            }

            if (!int.TryParse(Binders, out var binders) || binders < 0)
                throw new InvalidOperationException("Binders must be a non-negative whole number.");

            var editingFoodPhosphorusIntakeId = _editingFoodPhosphorusIntakeId;
            var shouldInsert = !editingFoodPhosphorusIntakeId.HasValue ||
                (_editingConsumedDate.HasValue && _editingConsumedDate.Value != ConsumedAt.Date);
            long? savedFoodPhosphorusIntakeId = shouldInsert
                ? null
                : editingFoodPhosphorusIntakeId;

            if (shouldInsert)
            {
                savedFoodPhosphorusIntakeId = _service.Insert(
                    SelectedPerson.PersonId,
                    FoodName.Trim(),
                    phosphorus,
                    calories,
                    sodiumMg,
                    proteinG,
                    potassiumMg,
                    null,
                    binders,
                    ConsumedAt,
                    null,
                    ServingDescription,
                    EstimatedByAi,
                    EstimatedByAi ? "OpenAI" : null,
                    AiConfidence,
                    SourceNotes,
                    Environment.UserName);
            }
            else
            {
                _service.Update(
                    editingFoodPhosphorusIntakeId!.Value,
                    SelectedPerson.PersonId,
                    FoodName.Trim(),
                    phosphorus,
                    calories,
                    sodiumMg,
                    proteinG,
                    potassiumMg,
                    _editingFluidMl,
                    binders,
                    ConsumedAt,
                    null,
                    ServingDescription,
                    EstimatedByAi,
                    EstimatedByAi ? "OpenAI" : null,
                    AiConfidence,
                    SourceNotes,
                    Environment.UserName);
            }

            LoadHistoryForSelectedPerson();
            SelectSavedHistoryItem(savedFoodPhosphorusIntakeId);

            RefreshSelectedDayTotal();
        }

        public string LoadFoodNote()
        {
            if (SelectedHistory == null)
                return string.Empty;

            return _service.GetFoodNote(GetOrCreateSelectedFoodProfileId());
        }

        public void SaveFoodNote(string noteText)
        {
            if (SelectedHistory == null)
                throw new InvalidOperationException("Save the food entry before editing food notes.");

            var foodPhosphorusFoodId = GetOrCreateSelectedFoodProfileId();
            var foodPhosphorusIntakeId = SelectedHistory.FoodPhosphorusIntakeId;

            _service.SaveFoodNote(foodPhosphorusFoodId, noteText);
            LoadHistoryForSelectedPerson();
            SelectSavedHistoryItem(foodPhosphorusIntakeId);
        }

        private long GetOrCreateSelectedFoodProfileId()
        {
            if (SelectedHistory == null)
                throw new InvalidOperationException("Select a food entry before editing food notes.");

            if (SelectedHistory.FoodPhosphorusFoodId.HasValue)
                return SelectedHistory.FoodPhosphorusFoodId.Value;

            var foodProfileId = _service.EnsureFoodProfileForIntake(SelectedHistory);
            SelectedHistory.FoodPhosphorusFoodId = foodProfileId;
            OnPropertyChanged(nameof(CanEditFoodNotes));
            return foodProfileId;
        }

        private void SelectSavedHistoryItem(long? savedFoodPhosphorusIntakeId)
        {
            if (!savedFoodPhosphorusIntakeId.HasValue)
                return;

            var savedItem = _allHistory
                .FirstOrDefault(item => item.FoodPhosphorusIntakeId == savedFoodPhosphorusIntakeId.Value);
            if (savedItem == null)
                return;

            if (!History.Contains(savedItem))
            {
                _searchText = string.Empty;
                OnPropertyChanged(nameof(SearchText));
                ApplyHistorySearch();
            }

            SelectedHistory = savedItem;
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

        private static bool IsOptionalNonNegativeWholeNumber(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                (int.TryParse(value, out var parsedValue) && parsedValue >= 0);
        }

        private static bool IsOptionalNonNegativeDecimal(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                (decimal.TryParse(value, out var parsedValue) && parsedValue >= 0);
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
