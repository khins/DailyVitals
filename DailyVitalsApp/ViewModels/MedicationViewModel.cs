using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DailyVitals.App.ViewModels
{
    public class MedicationViewModel : ViewModelBase
    {
        private readonly MedicationService _service = new();
        private readonly PersonService _personService = new();

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<Medication> Medications { get; }

        public bool IsEditMode { get; }
        public long MedicationId { get; }


        public Person SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                
            }
        }
        private Person _selectedPerson;

        public Medication SelectedMedication { get; set; }

        public string MedicationName { get; set; }
        public string DosageMg { get; set; }
        public string DosageForm { get; set; }

        public bool TakeMorning { get; set; }
        public bool TakeNoon { get; set; }
        public bool TakeEvening { get; set; }
        public bool TakeBedtime { get; set; }

        public string Instructions { get; set; }
        public string PrescribedBy { get; set; }

        public DateTime? StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; }

        // ✅ REQUIRED parameterless constructor
        public MedicationViewModel()
        {
            LoadPersons();
        }

        public MedicationViewModel(Medication medication) : this()
        {
            IsEditMode = true;
            MedicationId = medication.MedicationId;

            SelectedPerson = Persons.FirstOrDefault(
                    p => p.PersonId == medication.PersonId);

            MedicationName = medication.MedicationName;
            DosageMg = medication.DosageMg.ToString();
            DosageForm = medication.DosageForm;

            TakeMorning = medication.TakeMorning;
            TakeNoon = medication.TakeNoon;
            TakeEvening = medication.TakeEvening;
            TakeBedtime = medication.TakeBedtime;

            Instructions = medication.Instructions;
            PrescribedBy = medication.PrescribedBy;
        }


        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var p in _personService.GetAllPersons())
                Persons.Add(p);
        }

        private void LoadMedications()
        {
            Medications.Clear();

            if (SelectedPerson == null)
                return;

            foreach (var m in _service.GetMedications(SelectedPerson.PersonId))
                Medications.Add(m);
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
                    DosageForm,
                    TakeMorning,
                    TakeNoon,
                    TakeEvening,
                    TakeBedtime,
                    Instructions,
                    PrescribedBy
                );
            }
            else
            {
                _service.InsertMedication(
                    SelectedPerson.PersonId,
                    MedicationName,
                    dosage,
                    DosageForm,
                    TakeMorning,
                    TakeNoon,
                    TakeEvening,
                    TakeBedtime,
                    Instructions,
                    PrescribedBy,
                    StartDate,
                    EndDate
                );
            }
        }

    }

}
