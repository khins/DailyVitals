using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DailyVitals.App.ViewModels
{
    public class MedicationListViewModel : ViewModelBase
    {
        private readonly MedicationService _service = new();
        private readonly PersonService _personService = new();

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<Medication> Medications { get; } = new();

        public Person SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadMedications();
            }
        }
        private Person _selectedPerson;

        public Medication SelectedMedication { get; set; }

        public MedicationListViewModel()
        {
            LoadPersons();
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

        public void DeactivateSelected()
        {
            if (SelectedMedication == null)
                return;

            _service.DeactivateMedication(
                SelectedMedication.MedicationId,
                Environment.UserName);

            LoadMedications();
        }

        public void Refresh()
        {
            LoadMedications();
        }

    }

}
