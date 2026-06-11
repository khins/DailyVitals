using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class NutritionAnalyticsViewModel : ViewModelBase
    {
        private const double SmallChartWidth = 520;
        private const double WideChartWidth = 1080;
        private const double ChartHeight = 190;
        private const double WeightChartHeight = 160;
        private const double MonthlyBarChartHeight = 190;

        private readonly NutritionAnalyticsService _analyticsService = new();
        private readonly PersonService _personService = new();
        private Person? _selectedPerson;

        public NutritionAnalyticsViewModel()
        {
            LoadPersons();
        }

        public ObservableCollection<Person> Persons { get; } = new();
        public ObservableCollection<NutritionAnalyticsDailyRow> DailyRows { get; } = new();
        public ObservableCollection<ExerciseAnalyticsMonthlyRow> MonthlyExerciseRows { get; } = new();
        public ObservableCollection<MonthlyExerciseBar> MonthlyExerciseBars { get; } = new();
        public PointCollection CalorieBalancePoints { get; } = new();
        public PointCollection CaloriesInPoints { get; } = new();
        public PointCollection SodiumUsagePoints { get; } = new();
        public PointCollection PhosphorusUsagePoints { get; } = new();
        public PointCollection WeightPoints { get; } = new();
        public PointCollection ExerciseCaloriesPoints { get; } = new();
        public PointCollection ExerciseRollingPoints { get; } = new();

        public string SummaryText { get; private set; } = "Select a person to view nutrition analytics";
        public string CalorieChartSummary { get; private set; } = string.Empty;
        public string NutrientChartSummary { get; private set; } = string.Empty;
        public string WeightChartSummary { get; private set; } = string.Empty;
        public string StartWeightLabel { get; private set; } = string.Empty;
        public string EndWeightLabel { get; private set; } = string.Empty;
        public Brush WeightChartSummaryBrush { get; private set; } = Brushes.Black;
        public string ExerciseChartSummary { get; private set; } = string.Empty;
        public string ExerciseStartDurationLabel { get; private set; } = string.Empty;
        public string ExerciseCurrentDurationLabel { get; private set; } = string.Empty;
        public string MonthlyExerciseSummary { get; private set; } = string.Empty;

        public Person? SelectedPerson
        {
            get => _selectedPerson;
            set
            {
                _selectedPerson = value;
                OnPropertyChanged();
                LoadAnalytics();
            }
        }

        private void LoadPersons()
        {
            Persons.Clear();
            foreach (var person in _personService.GetAllPersons())
                Persons.Add(person);

            SelectedPerson = Persons.FirstOrDefault();
        }

        private void LoadAnalytics()
        {
            DailyRows.Clear();
            MonthlyExerciseRows.Clear();
            MonthlyExerciseBars.Clear();
            ClearCharts();

            if (SelectedPerson == null)
            {
                SummaryText = "Select a person to view nutrition analytics";
                NotifySummariesChanged();
                return;
            }

            var rows = _analyticsService.GetDailyAnalytics(SelectedPerson.PersonId);
            var exerciseSessions = _analyticsService.GetExerciseSessions(SelectedPerson.PersonId);
            var monthlyExerciseRows = _analyticsService.GetMonthlyExerciseAnalytics(SelectedPerson.PersonId);

            foreach (var row in rows.OrderByDescending(row => row.Date))
                DailyRows.Add(row);
            foreach (var row in monthlyExerciseRows.OrderByDescending(row => row.Month))
                MonthlyExerciseRows.Add(row);

            BuildCharts(rows);
            BuildExerciseCharts(exerciseSessions, monthlyExerciseRows);
            BuildSummaries(rows, exerciseSessions, monthlyExerciseRows);
        }

        private void BuildCharts(List<NutritionAnalyticsDailyRow> rows)
        {
            var activeRows = rows
                .Where(row =>
                    row.CaloriesIn > 0 ||
                    row.ExerciseCalories > 0 ||
                    row.SodiumMg > 0 ||
                    row.NetPhosphorusMg > 0 ||
                    row.WeightValue != null)
                .ToList();

            if (activeRows.Count == 0)
            {
                NotifyChartsChanged();
                return;
            }

            BuildSharedPoints(
                SmallChartWidth,
                ChartHeight,
                new List<(IReadOnlyList<decimal> Values, PointCollection Points)>
                {
                    (activeRows.Select(row => (decimal)row.CalorieBalance).ToList(), CalorieBalancePoints),
                    (activeRows.Select(row => (decimal)row.CaloriesIn).ToList(), CaloriesInPoints)
                });

            BuildSharedPoints(
                SmallChartWidth,
                ChartHeight,
                new List<(IReadOnlyList<decimal> Values, PointCollection Points)>
                {
                    (activeRows.Select(row => row.SodiumLimitMg > 0
                        ? (decimal)row.SodiumMg / row.SodiumLimitMg.Value * 100m
                        : 0m).ToList(), SodiumUsagePoints),
                    (activeRows.Select(row => row.PhosphorusLimitMg > 0
                        ? row.NetPhosphorusMg / row.PhosphorusLimitMg.Value * 100m
                        : 0m).ToList(), PhosphorusUsagePoints)
                });

            var weightRows = activeRows
                .Where(row => row.WeightValue != null)
                .Select(row => row.WeightValue!.Value)
                .ToList();
            BuildPoints(weightRows, WeightPoints, WideChartWidth, WeightChartHeight);

            NotifyChartsChanged();
        }

        private void BuildExerciseCharts(
            List<ExerciseAnalyticsSessionRow> exerciseSessions,
            List<ExerciseAnalyticsMonthlyRow> monthlyExerciseRows)
        {
            var calories = exerciseSessions
                .Select(row => row.CaloriesExpended)
                .ToList();
            var rolling = BuildRollingAverage(calories, 7);

            BuildSharedPoints(
                WideChartWidth,
                ChartHeight,
                new List<(IReadOnlyList<decimal> Values, PointCollection Points)>
                {
                    (calories, ExerciseCaloriesPoints),
                    (rolling, ExerciseRollingPoints)
                });

            BuildMonthlyBars(monthlyExerciseRows);
            NotifyChartsChanged();
        }

        private static List<decimal> BuildRollingAverage(IReadOnlyList<decimal> values, int window)
        {
            var rolling = new List<decimal>();
            for (var i = 0; i < values.Count; i++)
            {
                var start = Math.Max(0, i - window + 1);
                var count = i - start + 1;
                rolling.Add(values.Skip(start).Take(count).Average());
            }

            return rolling;
        }

        private void BuildMonthlyBars(List<ExerciseAnalyticsMonthlyRow> monthlyExerciseRows)
        {
            MonthlyExerciseBars.Clear();
            if (monthlyExerciseRows.Count == 0)
                return;

            var max = Math.Max(monthlyExerciseRows.Max(row => row.TotalCalories), 1m);
            const double barWidth = 88;
            const double gap = 34;

            for (var i = 0; i < monthlyExerciseRows.Count; i++)
            {
                var row = monthlyExerciseRows[i];
                var height = Math.Max((double)(row.TotalCalories / max) * MonthlyBarChartHeight, 2d);
                var left = 20 + i * (barWidth + gap);

                MonthlyExerciseBars.Add(new MonthlyExerciseBar
                {
                    Left = left,
                    Top = MonthlyBarChartHeight - height,
                    Width = barWidth,
                    Height = height,
                    LabelLeft = left,
                    ValueTop = Math.Max(MonthlyBarChartHeight - height - 22, 0),
                    Label = row.Month.ToString("MMM yy", CultureInfo.CurrentCulture),
                    ValueText = $"{row.TotalCalories:N0}"
                });
            }
        }

        private static void BuildSharedPoints(
            double width,
            double height,
            IReadOnlyList<(IReadOnlyList<decimal> Values, PointCollection Points)> series)
        {
            foreach (var item in series)
                item.Points.Clear();

            var allValues = series
                .SelectMany(item => item.Values)
                .ToList();

            if (allValues.Count == 0)
                return;

            var min = allValues.Min();
            var max = allValues.Max();
            foreach (var item in series)
                BuildPoints(item.Values, item.Points, width, height, min, max);
        }

        private static void BuildPoints(
            IReadOnlyList<decimal> values,
            PointCollection points,
            double width,
            double height)
        {
            points.Clear();
            if (values.Count == 0)
                return;

            BuildPoints(values, points, width, height, values.Min(), values.Max());
        }

        private static void BuildPoints(
            IReadOnlyList<decimal> values,
            PointCollection points,
            double width,
            double height,
            decimal min,
            decimal max)
        {
            points.Clear();
            if (values.Count == 0)
                return;

            var range = Math.Max((double)(max - min), 1d);
            var spacing = values.Count == 1
                ? width / 2d
                : width / (values.Count - 1d);

            for (var i = 0; i < values.Count; i++)
            {
                var x = values.Count == 1 ? width / 2d : i * spacing;
                var y = height - (((double)(values[i] - min) / range) * height);
                points.Add(new Point(x, y));
            }
        }

        private void BuildSummaries(
            List<NutritionAnalyticsDailyRow> rows,
            List<ExerciseAnalyticsSessionRow> exerciseSessions,
            List<ExerciseAnalyticsMonthlyRow> monthlyExerciseRows)
        {
            var activeRows = rows
                .Where(row => row.CaloriesIn > 0 || row.ExerciseCalories > 0)
                .ToList();

            if (activeRows.Count == 0)
            {
                SummaryText = exerciseSessions.Count == 0
                    ? "No calorie balance data available"
                    : $"{exerciseSessions.Count} exercise sessions available for analytics";
                CalorieChartSummary = string.Empty;
                NutrientChartSummary = string.Empty;
                WeightChartSummary = string.Empty;
                StartWeightLabel = string.Empty;
                EndWeightLabel = string.Empty;
                WeightChartSummaryBrush = Brushes.Black;
                ExerciseChartSummary = BuildExerciseSummary(exerciseSessions);
                BuildExerciseDurationLabels(exerciseSessions);
                MonthlyExerciseSummary = BuildMonthlyExerciseSummary(monthlyExerciseRows);
                NotifySummariesChanged();
                return;
            }

            var avgCalories = activeRows.Average(row => row.CaloriesIn);
            var avgExercise = activeRows.Average(row => row.ExerciseCalories);
            var avgBalance = activeRows.Average(row => row.CalorieBalance);
            var latest = rows.LastOrDefault(row => row.CaloriesIn > 0 || row.ExerciseCalories > 0);
            var weightRows = rows.Where(row => row.WeightValue != null).ToList();

            SummaryText = $"{activeRows.Count} active days, {avgCalories:F0} avg calories in, {avgExercise:F0} avg exercise calories";
            CalorieChartSummary = $"Average daily balance: {avgBalance:F0} calories";
            NutrientChartSummary = latest == null
                ? string.Empty
                : $"Latest day: {latest.SodiumMg:N0} mg sodium, {latest.NetPhosphorusMg:F0} mg net phosphorus";
            if (weightRows.Count >= 2)
            {
                var startWeight = weightRows[0].WeightValue!.Value;
                var endWeight = weightRows[^1].WeightValue!.Value;
                var weightChange = endWeight - startWeight;

                StartWeightLabel = $"Start: {startWeight:F1} lb";
                EndWeightLabel = $"Current: {endWeight:F1} lb";
                WeightChartSummary = $"Weight change: {weightChange:F1} lb";
                WeightChartSummaryBrush = weightChange < 0 ? Brushes.Green : Brushes.Red;
            }
            else if (weightRows.Count == 1)
            {
                var currentWeight = weightRows[0].WeightValue!.Value;

                StartWeightLabel = $"Current: {currentWeight:F1} lb";
                EndWeightLabel = $"Current: {currentWeight:F1} lb";
                WeightChartSummary = "Weight trend needs at least two readings";
                WeightChartSummaryBrush = Brushes.Black;
            }
            else
            {
                StartWeightLabel = string.Empty;
                EndWeightLabel = string.Empty;
                WeightChartSummary = "Weight trend needs at least two readings";
                WeightChartSummaryBrush = Brushes.Black;
            }
            ExerciseChartSummary = BuildExerciseSummary(exerciseSessions);
            BuildExerciseDurationLabels(exerciseSessions);
            MonthlyExerciseSummary = BuildMonthlyExerciseSummary(monthlyExerciseRows);

            NotifySummariesChanged();
        }

        private void BuildExerciseDurationLabels(List<ExerciseAnalyticsSessionRow> exerciseSessions)
        {
            if (exerciseSessions.Count == 0)
            {
                ExerciseStartDurationLabel = string.Empty;
                ExerciseCurrentDurationLabel = string.Empty;
                return;
            }

            ExerciseStartDurationLabel = $"Start duration: {exerciseSessions[0].DurationMinutes:F0} min";
            ExerciseCurrentDurationLabel = $"Current duration: {exerciseSessions[^1].DurationMinutes:F0} min";
        }

        private static string BuildExerciseSummary(List<ExerciseAnalyticsSessionRow> exerciseSessions)
        {
            if (exerciseSessions.Count == 0)
                return "No exercise sessions in the selected window";

            var average = exerciseSessions.Average(row => row.CaloriesExpended);
            var latest = exerciseSessions[^1];
            var max = exerciseSessions.Max(row => row.CaloriesExpended);
            var total = exerciseSessions.Sum(row => row.CaloriesExpended);
            var totalMinutes = exerciseSessions.Sum(row => row.DurationMinutes);
            var wholeTotalMinutes = (int)totalMinutes;
            var hours = wholeTotalMinutes / 60;
            var minutes = wholeTotalMinutes % 60;

            return $"{exerciseSessions.Count} sessions, {average:F0} avg calories, {latest.CaloriesExpended:F0} latest, {max:F0} high, {total:N0} total, Total Exercise Time: {hours:N0} hr {minutes} min";
        }

        private static string BuildMonthlyExerciseSummary(List<ExerciseAnalyticsMonthlyRow> monthlyExerciseRows)
        {
            if (monthlyExerciseRows.Count == 0)
                return "No monthly exercise calories available";

            var bestMonth = monthlyExerciseRows
                .OrderByDescending(row => row.TotalCalories)
                .First();

            return $"Top month: {bestMonth.Month:MMM yyyy} with {bestMonth.TotalCalories:N0} calories burned";
        }

        private void ClearCharts()
        {
            CalorieBalancePoints.Clear();
            CaloriesInPoints.Clear();
            SodiumUsagePoints.Clear();
            PhosphorusUsagePoints.Clear();
            WeightPoints.Clear();
            ExerciseCaloriesPoints.Clear();
            ExerciseRollingPoints.Clear();
            NotifyChartsChanged();
        }

        private void NotifyChartsChanged()
        {
            OnPropertyChanged(nameof(CalorieBalancePoints));
            OnPropertyChanged(nameof(CaloriesInPoints));
            OnPropertyChanged(nameof(SodiumUsagePoints));
            OnPropertyChanged(nameof(PhosphorusUsagePoints));
            OnPropertyChanged(nameof(WeightPoints));
            OnPropertyChanged(nameof(ExerciseCaloriesPoints));
            OnPropertyChanged(nameof(ExerciseRollingPoints));
            OnPropertyChanged(nameof(MonthlyExerciseBars));
        }

        private void NotifySummariesChanged()
        {
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(CalorieChartSummary));
            OnPropertyChanged(nameof(NutrientChartSummary));
            OnPropertyChanged(nameof(WeightChartSummary));
            OnPropertyChanged(nameof(StartWeightLabel));
            OnPropertyChanged(nameof(EndWeightLabel));
            OnPropertyChanged(nameof(WeightChartSummaryBrush));
            OnPropertyChanged(nameof(ExerciseChartSummary));
            OnPropertyChanged(nameof(ExerciseStartDurationLabel));
            OnPropertyChanged(nameof(ExerciseCurrentDurationLabel));
            OnPropertyChanged(nameof(MonthlyExerciseSummary));
        }
    }

    public class MonthlyExerciseBar
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double LabelLeft { get; set; }
        public double ValueTop { get; set; }
        public string Label { get; set; } = string.Empty;
        public string ValueText { get; set; } = string.Empty;
    }
}
