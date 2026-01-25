using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using DailyVitals.Domain.Models.Calculations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DailyVitals.App.ViewModels
{
    public class VitalsDashboardViewModel : ViewModelBase
    {
        private readonly PersonService _personService = new();
        private readonly BloodPressureService _bpService = new();
        private readonly BloodGlucoseService _bgService = new();
        private readonly WeightService _weightService = new();

        public ObservableCollection<Person> Persons { get; } = new();

        private Person _selectedPerson;
        public Person SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadLatestVitals();
            }
        }

        public BloodPressureReading LatestBP { get; private set; }
        public BloodGlucoseReading LatestGlucose { get; private set; }
        public WeightReading LatestWeight { get; private set; }

        public decimal? BMI =>
            LatestWeight?.HeightFt == null
                ? null
                : HealthMetrics.CalculateBMI(
                    LatestWeight.WeightValue,
                    LatestWeight.HeightFt.Value);

        public VitalsDashboardViewModel()
        {
            LoadPersons();
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var p in _personService.GetAllPersons())
                Persons.Add(p);
        }

        private void LoadLatestVitals()
        {
            if (SelectedPerson == null) return;

            LatestBP = _bpService.GetLatestForPerson(SelectedPerson.PersonId);
            LatestGlucose = _bgService.GetLatestForPerson(SelectedPerson.PersonId);
            LatestWeight = _weightService.GetLatestForPerson(SelectedPerson.PersonId);

            OnPropertyChanged(nameof(LatestBP));
            OnPropertyChanged(nameof(LatestGlucose));
            OnPropertyChanged(nameof(LatestWeight));
            OnPropertyChanged(nameof(BMI));
        }
    }

}
