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
        private readonly List<FoodPhosphorusRunningTotal> _allRows = new();
        private string _searchText = string.Empty;

        public FoodPhosphorusRunningTotalsViewModel(long personId, string personName)
        {
            PersonName = personName;
            Load(personId);
        }

        public string PersonName { get; }
        public ObservableCollection<FoodPhosphorusRunningTotal> Rows { get; } = new();
        public ObservableCollection<FoodPhosphorusMonthlyTotal> MonthlyTotals { get; } = new();

        public string SummaryText { get; private set; } = "No food phosphorus entries available";
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
            MonthlyTotals.Clear();
            _allRows.Clear();

            var rows = _service.GetRunningDailyTotals(personId);
            foreach (var row in rows)
                _allRows.Add(row);

            ApplySearch();

            if (rows.Count == 0)
                return;

            var latestDay = rows
                .GroupBy(row => row.IntakeDate)
                .OrderByDescending(group => group.Key)
                .First();
            var latestRow = latestDay
                .OrderBy(row => row.ConsumedAt)
                .Last();

            SummaryText = $"{latestDay.Key:d}: {latestRow.RunningNetDailyMg:F0} net mg phosphorus, {latestRow.RunningDailyCalories} calories, {latestRow.RunningDailySodiumMg} mg sodium";

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

        public class FoodPhosphorusMonthlyTotal
        {
            public string Month { get; set; } = string.Empty;
            public int Entries { get; set; }
            public int RawPhosphorusMg { get; set; }
            public decimal NetPhosphorusMg { get; set; }
            public int Calories { get; set; }
            public int SodiumMg { get; set; }
            public int PillsTaken { get; set; }
        }
    }
}
