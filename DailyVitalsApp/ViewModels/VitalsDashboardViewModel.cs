using System.Windows;
using System.Windows.Media;
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
        public ObservableCollection<Point> WeightTrendPoints { get; } = new();

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
            OnPropertyChanged(nameof(BMIBrush));
        }
        public Brush BMIBrush
        {
            get
            {
                if (LatestWeight?.HeightFt == null)
                    return Brushes.Transparent;

                var bmi = HealthMetrics.CalculateBMI(
                    LatestWeight.WeightValue,
                    LatestWeight.HeightFt.Value);

                if (bmi < 18.5m) return Brushes.LightBlue;
                if (bmi < 25.0m) return Brushes.LightGreen;
                if (bmi < 30.0m) return Brushes.Khaki;
                return Brushes.IndianRed;
            }
        }


        private void BuildWeightTrend(IEnumerable<TrendPoint> trend)
        {
            WeightTrendPoints.Clear();

            var values = trend.Select(t => (double)t.Value).ToList();
            if (values.Count < 2)
                return;

            double min = values.Min();
            double max = values.Max();
            double range = Math.Max(max - min, 1); // avoid divide-by-zero

            double width = 120;
            double height = 30;

            for (int i = 0; i < values.Count; i++)
            {
                double x = i * (width / (values.Count - 1));
                double y = height - ((values[i] - min) / range * height);
                WeightTrendPoints.Add(new Point(x, y));
            }
        }



    }

}
