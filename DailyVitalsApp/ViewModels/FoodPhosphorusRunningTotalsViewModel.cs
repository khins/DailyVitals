using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyVitals.App.ViewModels
{
    public class FoodPhosphorusRunningTotalsViewModel : ViewModelBase
    {
        private readonly FoodPhosphorusIntakeService _service = new();
        private readonly NutritionGoalService _nutritionGoalService = new();
        private readonly List<FoodPhosphorusRunningTotal> _allRows = new();
        private string _searchText = string.Empty;

        public FoodPhosphorusRunningTotalsViewModel(long personId, string personName)
        {
            PersonName = personName;
            Load(personId);
        }

        public string PersonName { get; }
        public ObservableCollection<FoodPhosphorusRunningTotal> Rows { get; } = new();
        public ObservableCollection<DailyNutritionTargetRow> DailyNutritionTargets { get; } = new();
        public ObservableCollection<FoodPhosphorusMonthlyTotal> MonthlyTotals { get; } = new();

        public string SummaryText { get; private set; } = "No food phosphorus entries available";
        public string NutritionGoalSummary { get; private set; } = "No active nutrition goal";
        public string SearchResultsText { get; private set; } = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;
                OnPropertyChanged();
                ApplySearch();
            }
        }

        private void Load(long personId)
        {
            Rows.Clear();
            DailyNutritionTargets.Clear();
            MonthlyTotals.Clear();
            _allRows.Clear();

            var rows = _service.GetRunningDailyTotals(personId);
            foreach (var row in rows)
                _allRows.Add(row);

            ApplySearch();

            if (rows.Count == 0)
            {
                LoadNutritionGoal(personId, DateTime.Today, null);
                return;
            }

            var latestDay = rows
                .GroupBy(row => row.IntakeDate)
                .OrderByDescending(group => group.Key)
                .First();
            var latestTotals = BuildDailyTotals(latestDay);

            SummaryText = $"{latestDay.Key:d}: {latestTotals.NetPhosphorusMg:F0} net mg phosphorus, {latestTotals.Calories} calories, {latestTotals.SodiumMg} mg sodium";
            LoadNutritionGoal(personId, latestDay.Key, latestTotals);

            foreach (var month in rows
                .GroupBy(row => new DateTime(row.IntakeDate.Year, row.IntakeDate.Month, 1))
                .OrderByDescending(group => group.Key))
            {
                MonthlyTotals.Add(new FoodPhosphorusMonthlyTotal
                {
                    Month = month.Key.ToString("MMM yyyy"),
                    Entries = month.Count(),
                    RawPhosphorusMg = month.Sum(row => row.RawPhosphorusMg),
                    NetPhosphorusMg = month.Sum(row => row.NetItemPhosphorusMg),
                    Calories = month.Sum(row => row.Calories),
                    SodiumMg = month.Sum(row => row.SodiumMg),
                    ProteinG = month.Sum(row => row.ProteinG),
                    PotassiumMg = month.Sum(row => row.PotassiumMg),
                    FluidMl = month.Sum(row => row.FluidMl),
                    PillsTaken = month.Sum(row => row.PillsTaken)
                });
            }
        }

        private void ApplySearch()
        {
            Rows.Clear();

            var searchText = SearchText.Trim();
            var filteredRows = string.IsNullOrWhiteSpace(searchText)
                ? _allRows
                : _allRows
                    .Where(row => row.FoodName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            foreach (var row in filteredRows
                .OrderByDescending(row => row.IntakeDate)
                .ThenByDescending(row => row.RunningNetDailyMg)
                .ThenByDescending(row => row.RunningDailyCalories)
                .ThenByDescending(row => row.ConsumedAt))
                Rows.Add(row);

            SearchResultsText = string.IsNullOrWhiteSpace(searchText)
                ? $"{_allRows.Count} entries"
                : $"{filteredRows.Count} of {_allRows.Count} entries";
            OnPropertyChanged(nameof(SearchResultsText));
        }

        private void LoadNutritionGoal(long personId, DateTime intakeDate, DailyNutritionTotals? latestTotals)
        {
            var goal = _nutritionGoalService.GetActiveGoal(personId, intakeDate);
            if (goal == null)
            {
                NutritionGoalSummary = $"No nutrition goal effective on {intakeDate:d}";
                OnPropertyChanged(nameof(NutritionGoalSummary));
                return;
            }

            NutritionGoalSummary = $"Effective {goal.EffectiveDate:d}";
            OnPropertyChanged(nameof(NutritionGoalSummary));

            DailyNutritionTargets.Add(BuildTargetRow(
                "Net Phosphorus",
                latestTotals?.NetPhosphorusMg,
                goal.PhosphorusLimitMg,
                "mg"));
            DailyNutritionTargets.Add(BuildTargetRow(
                "Sodium",
                latestTotals?.SodiumMg,
                goal.SodiumLimitMg,
                "mg"));
            DailyNutritionTargets.Add(BuildTargetRow(
                "Calories",
                latestTotals?.Calories,
                goal.CalorieLimit,
                "cal"));

            if (goal.ProteinTargetG != null)
            {
                DailyNutritionTargets.Add(BuildTargetRow(
                    "Protein",
                    latestTotals?.ProteinG,
                    goal.ProteinTargetG.Value,
                    "g"));
            }

            if (goal.PotassiumLimitMg != null)
            {
                DailyNutritionTargets.Add(BuildTargetRow(
                    "Potassium",
                    latestTotals?.PotassiumMg,
                    goal.PotassiumLimitMg.Value,
                    "mg"));
            }

            if (goal.FluidLimitMl != null)
            {
                DailyNutritionTargets.Add(BuildTargetRow(
                    "Fluid",
                    latestTotals?.FluidMl,
                    goal.FluidLimitMl.Value,
                    "ml"));
            }
        }

        private static DailyNutritionTotals BuildDailyTotals(IEnumerable<FoodPhosphorusRunningTotal> rows)
        {
            return new DailyNutritionTotals
            {
                NetPhosphorusMg = rows.Sum(row => row.NetItemPhosphorusMg),
                Calories = rows.Sum(row => row.Calories),
                SodiumMg = rows.Sum(row => row.SodiumMg),
                ProteinG = rows.Sum(row => row.ProteinG),
                PotassiumMg = rows.Sum(row => row.PotassiumMg),
                FluidMl = rows.Sum(row => row.FluidMl)
            };
        }

        private static DailyNutritionTargetRow BuildTargetRow(
            string nutrient,
            decimal? consumed,
            decimal target,
            string unit)
        {
            var remaining = target - (consumed ?? 0);
            return new DailyNutritionTargetRow
            {
                Nutrient = nutrient,
                Consumed = consumed == null ? "-" : $"{consumed.Value:N0} {unit}",
                Target = $"{target:N0} {unit}",
                Remaining = $"{remaining:N0} {unit}"
            };
        }

        public class FoodPhosphorusMonthlyTotal
        {
            public string Month { get; set; } = string.Empty;
            public int Entries { get; set; }
            public int RawPhosphorusMg { get; set; }
            public decimal NetPhosphorusMg { get; set; }
            public int Calories { get; set; }
            public int SodiumMg { get; set; }
            public decimal ProteinG { get; set; }
            public int PotassiumMg { get; set; }
            public int FluidMl { get; set; }
            public int PillsTaken { get; set; }
        }

        private class DailyNutritionTotals
        {
            public decimal NetPhosphorusMg { get; set; }
            public int Calories { get; set; }
            public int SodiumMg { get; set; }
            public decimal ProteinG { get; set; }
            public int PotassiumMg { get; set; }
            public int FluidMl { get; set; }
        }
    }
}
