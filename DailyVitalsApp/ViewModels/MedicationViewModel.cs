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

        public Person SelectedPerson { get; set; }

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

        public MedicationViewModel()
        {
            LoadPersons();
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var p in _personService.GetAllPersons())
                Persons.Add(p);
        }

        public void Save()
        {
            if (SelectedPerson == null)
                throw new InvalidOperationException("Select a person.");

            if (!decimal.TryParse(DosageMg, out var dosage))
                throw new InvalidOperationException("Dosage must be numeric.");

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

            ClearForm();
        }

        private void ClearForm()
        {
            MedicationName = string.Empty;
            DosageMg = string.Empty;
            DosageForm = string.Empty;
            Instructions = string.Empty;
            PrescribedBy = string.Empty;

            TakeMorning = TakeNoon = TakeEvening = TakeBedtime = false;

            StartDate = DateTime.Today;
            EndDate = null;

            OnPropertyChanged(string.Empty);
        }
    }

}
