using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;

namespace DailyVitals.App.ViewModels
{
    public class MedicationListViewModel : ViewModelBase
    {
        private readonly MedicationService _service = new();
        private readonly PersonService _personService = new();

        private Person? _selectedPerson;
        private Medication? _selectedMedication;

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<Medication> Medications { get; } = new();

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadMedications();
            }
        }

        public Medication? SelectedMedication
        {
            get => _selectedMedication;
            set
            {
                _selectedMedication = value;
                OnPropertyChanged();
            }
        }

        public MedicationListViewModel()
        {
            LoadPersons();
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
