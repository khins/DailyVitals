using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace DailyVitals.App.ViewModels
{
    public class KidneyLabResultsViewModel : ViewModelBase
    {
        private readonly KidneyLabResultService _service = new();
        private readonly PersonService _personService = new();

        private Person? _selectedPerson;
        private KidneyLabResult? _selectedHistory;
        private string _albumin = string.Empty;
        private string _npcr = string.Empty;
        private string _potassium = string.Empty;
        private string _wKtV = string.Empty;
        private string _calcium = string.Empty;
        private string _phosphorus = string.Empty;
        private string _iPTH = string.Empty;
        private string _hemoglobin = string.Empty;
        private string _glucose = string.Empty;
        private string _cholesterol = string.Empty;
        private string _triglycerides = string.Empty;
        private string _bun = string.Empty;
        private string _creatinine = string.Empty;

        public KidneyLabResultsViewModel()
        {
            LoadPersons();
            BeginNew();
        }

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<KidneyLabResult> History { get; } = new();

        public bool CanDelete => SelectedHistory != null;
        public bool CanSave =>
            !string.IsNullOrWhiteSpace(Albumin) &&
            !string.IsNullOrWhiteSpace(NPCR) &&
            !string.IsNullOrWhiteSpace(Potassium) &&
            !string.IsNullOrWhiteSpace(WKtV) &&
            !string.IsNullOrWhiteSpace(Calcium) &&
            !string.IsNullOrWhiteSpace(Phosphorus) &&
            !string.IsNullOrWhiteSpace(IPTH) &&
            !string.IsNullOrWhiteSpace(Hemoglobin) &&
            !string.IsNullOrWhiteSpace(Glucose) &&
            !string.IsNullOrWhiteSpace(Cholesterol) &&
            !string.IsNullOrWhiteSpace(Triglycerides) &&
            !string.IsNullOrWhiteSpace(BUN) &&
            !string.IsNullOrWhiteSpace(Creatinine);

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

        public KidneyLabResult? SelectedHistory
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

        public DateTime ResultMonth { get; set; }
        public string? Notes { get; set; }

        public string Albumin { get => _albumin; set => SetDecimalText(ref _albumin, value, nameof(Albumin)); }
        public string NPCR { get => _npcr; set => SetDecimalText(ref _npcr, value, nameof(NPCR)); }
        public string Potassium { get => _potassium; set => SetDecimalText(ref _potassium, value, nameof(Potassium)); }
        public string WKtV { get => _wKtV; set => SetDecimalText(ref _wKtV, value, nameof(WKtV)); }
        public string Calcium { get => _calcium; set => SetDecimalText(ref _calcium, value, nameof(Calcium)); }
        public string Phosphorus { get => _phosphorus; set => SetDecimalText(ref _phosphorus, value, nameof(Phosphorus)); }
        public string IPTH { get => _iPTH; set => SetDecimalText(ref _iPTH, value, nameof(IPTH)); }
        public string Hemoglobin { get => _hemoglobin; set => SetDecimalText(ref _hemoglobin, value, nameof(Hemoglobin)); }
        public string Glucose { get => _glucose; set => SetDecimalText(ref _glucose, value, nameof(Glucose)); }
        public string Cholesterol { get => _cholesterol; set => SetDecimalText(ref _cholesterol, value, nameof(Cholesterol)); }
        public string Triglycerides { get => _triglycerides; set => SetDecimalText(ref _triglycerides, value, nameof(Triglycerides)); }
        public string BUN { get => _bun; set => SetDecimalText(ref _bun, value, nameof(BUN)); }
        public string Creatinine { get => _creatinine; set => SetDecimalText(ref _creatinine, value, nameof(Creatinine)); }

        public void BeginNew()
        {
            SelectedHistory = null;
            ResultMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            Albumin = string.Empty;
            NPCR = string.Empty;
            Potassium = string.Empty;
            WKtV = string.Empty;
            Calcium = string.Empty;
            Phosphorus = string.Empty;
            IPTH = string.Empty;
            Hemoglobin = string.Empty;
            Glucose = string.Empty;
            Cholesterol = string.Empty;
            Triglycerides = string.Empty;
            BUN = string.Empty;
            Creatinine = string.Empty;
            Notes = "Monthly kidney lab panel";

            OnPropertyChanged(nameof(ResultMonth));
            OnPropertyChanged(nameof(Notes));
        }

        public void Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Please select a person.");

            var normalizedMonth = NormalizeMonth(ResultMonth);

            if (SelectedHistory == null &&
                History.Any(item => NormalizeMonth(item.ResultMonth) == normalizedMonth))
            {
                throw new InvalidOperationException(
                    $"A kidney lab result already exists for {normalizedMonth:MMM yyyy}. Select it from history to update it.");
            }

            var result = new KidneyLabResult
            {
                KidneyLabResultId = SelectedHistory?.KidneyLabResultId ?? 0,
                PersonId = SelectedPerson.PersonId,
                ResultMonth = normalizedMonth,
                Albumin = ParseRequiredDecimal(Albumin, "Albumin"),
                NPCR = ParseRequiredDecimal(NPCR, "nPCR"),
                Potassium = ParseRequiredDecimal(Potassium, "Potassium"),
                WKtV = ParseRequiredDecimal(WKtV, "wKt/V"),
                Calcium = ParseRequiredDecimal(Calcium, "Calcium"),
                Phosphorus = ParseRequiredDecimal(Phosphorus, "Phosphorus"),
                IPTH = ParseRequiredDecimal(IPTH, "iPTH"),
                Hemoglobin = ParseRequiredDecimal(Hemoglobin, "Hemoglobin"),
                Glucose = ParseRequiredDecimal(Glucose, "Glucose"),
                Cholesterol = ParseRequiredDecimal(Cholesterol, "Cholesterol"),
                Triglycerides = ParseRequiredDecimal(Triglycerides, "Triglycerides"),
                BUN = ParseRequiredDecimal(BUN, "BUN"),
                Creatinine = ParseRequiredDecimal(Creatinine, "Creatinine"),
                Notes = Notes
            };

            if (SelectedHistory == null)
                _service.Insert(result, Environment.UserName);
            else
                _service.Update(result, Environment.UserName);

            LoadHistory();
            BeginNew();
        }

        public void DeleteSelected()
        {
            if (SelectedHistory == null)
                return;

            _service.Delete(SelectedHistory.KidneyLabResultId, Environment.UserName);
            LoadHistory();
            BeginNew();
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

            foreach (var result in _service.GetHistory(SelectedPerson.PersonId))
                History.Add(result);
        }

        private void LoadFromHistory()
        {
            if (SelectedHistory == null)
                return;

            ResultMonth = NormalizeMonth(SelectedHistory.ResultMonth);
            Albumin = SelectedHistory.Albumin.ToString("0.##", CultureInfo.CurrentCulture);
            NPCR = SelectedHistory.NPCR.ToString("0.##", CultureInfo.CurrentCulture);
            Potassium = SelectedHistory.Potassium.ToString("0.##", CultureInfo.CurrentCulture);
            WKtV = SelectedHistory.WKtV.ToString("0.##", CultureInfo.CurrentCulture);
            Calcium = SelectedHistory.Calcium.ToString("0.##", CultureInfo.CurrentCulture);
            Phosphorus = SelectedHistory.Phosphorus.ToString("0.##", CultureInfo.CurrentCulture);
            IPTH = SelectedHistory.IPTH.ToString("0.##", CultureInfo.CurrentCulture);
            Hemoglobin = SelectedHistory.Hemoglobin.ToString("0.##", CultureInfo.CurrentCulture);
            Glucose = SelectedHistory.Glucose.ToString("0.##", CultureInfo.CurrentCulture);
            Cholesterol = SelectedHistory.Cholesterol.ToString("0.##", CultureInfo.CurrentCulture);
            Triglycerides = SelectedHistory.Triglycerides.ToString("0.##", CultureInfo.CurrentCulture);
            BUN = SelectedHistory.BUN.ToString("0.##", CultureInfo.CurrentCulture);
            Creatinine = SelectedHistory.Creatinine.ToString("0.##", CultureInfo.CurrentCulture);
            Notes = SelectedHistory.Notes;

            OnPropertyChanged(nameof(ResultMonth));
            OnPropertyChanged(nameof(Notes));
        }

        private void SetDecimalText(ref string field, string value, string propertyName)
        {
            field = value;
            OnPropertyChanged(propertyName);
            OnPropertyChanged(nameof(CanSave));
        }

        private static decimal ParseRequiredDecimal(string value, string fieldName)
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) &&
                !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                throw new InvalidOperationException($"Invalid {fieldName} value.");
            }

            return parsed;
        }

        private static DateTime NormalizeMonth(DateTime date) =>
            new(date.Year, date.Month, 1);
    }
}
