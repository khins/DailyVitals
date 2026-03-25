using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyVitals.App.ViewModels
{
    public class MedicationViewModel : ViewModelBase
    {
        private readonly MedicationService _service = new();
        private readonly PersonService _personService = new();

        private Person? _selectedPerson;

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<Medication> Medications { get; } = new();

        public bool IsEditMode { get; }
        public long MedicationId { get; }

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
            }
        }

        public Medication? SelectedMedication { get; set; }

        public string MedicationName { get; set; } = string.Empty;
        public string DosageMg { get; set; } = string.Empty;
        public string? DosageForm { get; set; }
        public bool TakeMorning { get; set; }
        public bool TakeNoon { get; set; }
        public bool TakeEvening { get; set; }
        public bool TakeBedtime { get; set; }
        public string? Instructions { get; set; }
        public string? PrescribedBy { get; set; }
        public DateTime? StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; }

        public MedicationViewModel()
        {
            LoadPersons();
        }

        public MedicationViewModel(Medication medication) : this()
        {
            IsEditMode = true;
            MedicationId = medication.MedicationId;

            SelectedPerson = Persons.FirstOrDefault(
                person => person.PersonId == medication.PersonId);

            MedicationName = medication.MedicationName;
            DosageMg = medication.DosageMg.ToString();
            DosageForm = medication.DosageForm;
            TakeMorning = medication.TakeMorning;
            TakeNoon = medication.TakeNoon;
            TakeEvening = medication.TakeEvening;
            TakeBedtime = medication.TakeBedtime;
            Instructions = medication.Instructions;
            PrescribedBy = medication.PrescribedBy;
            StartDate = medication.StartDate;
            EndDate = medication.EndDate;
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var person in _personService.GetAllPersons())
                Persons.Add(person);
        }

        private void LoadMedications()
        {
            Medications.Clear();

            if (SelectedPerson == null)
                return;

            foreach (var medication in _service.GetMedications(SelectedPerson.PersonId))
                Medications.Add(medication);
        }

        public void Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Select a person.");

            if (!decimal.TryParse(DosageMg, out var dosage))
                throw new InvalidOperationException("Dosage must be numeric.");

            if (IsEditMode)
            {
                _service.UpdateMedication(
                    MedicationId,
                    dosage,
                    DosageForm ?? string.Empty,
                    TakeMorning,
                    TakeNoon,
                    TakeEvening,
                    TakeBedtime,
                    Instructions ?? string.Empty,
                    PrescribedBy ?? string.Empty
                );
            }
            else
            {
                _service.InsertMedication(
                    SelectedPerson.PersonId,
                    MedicationName,
                    dosage,
                    DosageForm ?? string.Empty,
                    TakeMorning,
                    TakeNoon,
                    TakeEvening,
                    TakeBedtime,
                    Instructions ?? string.Empty,
                    PrescribedBy ?? string.Empty,
                    StartDate,
                    EndDate
                );
            }
        }
    }
}
