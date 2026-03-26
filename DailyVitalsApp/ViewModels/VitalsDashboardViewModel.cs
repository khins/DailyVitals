using DailyVitals.Data.Services;
using DailyVitals.Data.Services.DailyVitals.App.Services;
using DailyVitals.Domain.Models;
using DailyVitals.Domain.Models.Calculations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class VitalsDashboardViewModel : ViewModelBase
    {
        private readonly PersonService _personService = new();
        private readonly BloodPressureService _bpService = new();
        private readonly BloodGlucoseService _bgService = new();
        private readonly WeightService _weightService = new();
        private readonly ExerciseService _exerciseService = new();

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<Point> WeightTrendPoints { get; } = new();

        private decimal? _weightTrendDelta;
        private Person? _selectedPerson;
        private int _lastWeekExerciseMinutes;
        private int _weeklyExerciseMinutes;

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadLatestVitals();
            }
        }

        public string ExerciseTrendArrow =>
            WeeklyExerciseMinutes > LastWeekExerciseMinutes ? "▲" :
            WeeklyExerciseMinutes < LastWeekExerciseMinutes ? "▼" :
            "▶";

        public Brush ExerciseTrendBrush =>
            WeeklyExerciseMinutes > LastWeekExerciseMinutes ? Brushes.Green :
            WeeklyExerciseMinutes < LastWeekExerciseMinutes ? Brushes.Red :
            Brushes.Gray;

        public int LastWeekExerciseMinutes
        {
            get => _lastWeekExerciseMinutes;
            set
            {
                if (_lastWeekExerciseMinutes != value)
                {
                    _lastWeekExerciseMinutes = value;
                    OnPropertyChanged();
                }
            }
        }

        public BloodPressureReading? LatestBP { get; private set; }
        public BloodGlucoseReading? LatestGlucose { get; private set; }
        public WeightReading? LatestWeight { get; private set; }
        public ExerciseSession? LatestExercise { get; private set; }
        public decimal? EstimatedCaloriesBurned { get; private set; }

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

        public int WeeklyExerciseMinutes
        {
            get => _weeklyExerciseMinutes;
            set
            {
                _weeklyExerciseMinutes = value;
                OnPropertyChanged();
            }
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

        public string ExerciseCardSummary =>
            LatestExercise == null
                ? "No exercise logged yet"
                : $"{LatestExercise.ExerciseName} · {LatestExercise.DurationMinutes:F0} min";

        public string ExerciseCardDate =>
            LatestExercise?.StartTime.ToString("d") ?? string.Empty;

        public string ExerciseCaloriesText =>
            EstimatedCaloriesBurned == null
                ? "Calories estimate unavailable"
                : $"Est. calories: {EstimatedCaloriesBurned.Value:F0}";

        public string ExerciseIntensityText =>
            LatestExercise == null
                ? string.Empty
                : $"Intensity: {LatestExercise.Intensity}";

        public string WeightTrendArrow
        {
            get
            {
                if (LatestWeight == null)
                    return string.Empty;

                if (_weightTrendDelta == null)
                    return "→";

                var delta = _weightTrendDelta.Value;
                if (delta > 0) return "↑";
                if (delta < 0) return "↓";
                return "→";
            }
        }

        public Brush WeightTrendBrush
        {
            get
            {
                if (LatestWeight == null || _weightTrendDelta == null)
                    return Brushes.Gray;

                var delta = _weightTrendDelta.Value;
                if (delta > 0) return Brushes.IndianRed;
                if (delta < 0) return Brushes.ForestGreen;
                return Brushes.Gray;
            }
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var person in _personService.GetAllPersons())
                Persons.Add(person);
        }

        private void LoadLatestVitals()
        {
            WeightTrendPoints.Clear();
            _weightTrendDelta = null;

            if (SelectedPerson == null)
                return;

            LatestBP = _bpService.GetLatestForPerson(SelectedPerson.PersonId);
            LatestGlucose = _bgService.GetLatestForPerson(SelectedPerson.PersonId);
            LatestWeight = _weightService.GetLatestForPerson(SelectedPerson.PersonId);
            WeeklyExerciseMinutes = _exerciseService.GetWeeklyTotalMinutes(SelectedPerson.PersonId);
            LastWeekExerciseMinutes = _exerciseService.GetLastWeekTotalMinutes(SelectedPerson.PersonId);
            var exerciseHistory = _exerciseService.GetHistory(SelectedPerson.PersonId);
            LatestExercise = exerciseHistory.FirstOrDefault();
            EstimatedCaloriesBurned = LatestExercise == null || LatestWeight == null
                ? null
                : ExerciseMetrics.EstimateCaloriesBurned(
                    LatestExercise.DurationMinutes,
                    LatestExercise.Intensity,
                    LatestWeight.WeightValue,
                    LatestWeight.WeightUnit);

            var weightTrend = _weightService.GetWeightTrend(SelectedPerson.PersonId, 2);
            if (weightTrend.Count >= 2)
                _weightTrendDelta = weightTrend[^1].Value - weightTrend[0].Value;

            BuildWeightTrend(weightTrend);

            OnPropertyChanged(nameof(LatestBP));
            OnPropertyChanged(nameof(LatestGlucose));
            OnPropertyChanged(nameof(LatestWeight));
            OnPropertyChanged(nameof(LatestExercise));
            OnPropertyChanged(nameof(BMI));
            OnPropertyChanged(nameof(BMIBrush));
            OnPropertyChanged(nameof(WeightTrendArrow));
            OnPropertyChanged(nameof(WeightTrendBrush));
            OnPropertyChanged(nameof(ExerciseTrendArrow));
            OnPropertyChanged(nameof(ExerciseTrendBrush));
            OnPropertyChanged(nameof(ExerciseCardSummary));
            OnPropertyChanged(nameof(ExerciseCardDate));
            OnPropertyChanged(nameof(ExerciseCaloriesText));
            OnPropertyChanged(nameof(ExerciseIntensityText));
        }

        private void BuildWeightTrend(IEnumerable<TrendPoint> trend)
        {
            WeightTrendPoints.Clear();

            var values = trend.Select(t => (double)t.Value).ToList();
            if (values.Count < 2)
                return;

            double min = values.Min();
            double max = values.Max();
            double range = Math.Max(max - min, 1);
            const double width = 120;
            const double height = 30;

            for (int i = 0; i < values.Count; i++)
            {
                double x = i * (width / (values.Count - 1));
                double y = height - ((values[i] - min) / range * height);
                WeightTrendPoints.Add(new Point(x, y));
            }
        }
    }
}
