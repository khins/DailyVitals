using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DailyVitals.App.ViewModels
{
    public class FoodPhosphorusRunningTotalsViewModel : ViewModelBase
    {
        private readonly FoodPhosphorusIntakeService _service = new();

        public FoodPhosphorusRunningTotalsViewModel(long personId, string personName)
        {
            PersonName = personName;
            Load(personId);
        }

        public string PersonName { get; }
        public ObservableCollection<FoodPhosphorusRunningTotal> Rows { get; } = new();
        public ObservableCollection<FoodPhosphorusMonthlyTotal> MonthlyTotals { get; } = new();

        public string SummaryText { get; private set; } = "No food phosphorus entries available";

        private void Load(long personId)
        {
            Rows.Clear();
            MonthlyTotals.Clear();

            var rows = _service.GetRunningDailyTotals(personId);
            foreach (var row in rows)
                Rows.Add(row);

            if (rows.Count == 0)
                return;

            var latestDay = rows
                .GroupBy(row => row.IntakeDate)
                .OrderByDescending(group => group.Key)
                .First();
            var latestRow = latestDay
                .OrderBy(row => row.ConsumedAt)
                .Last();

            SummaryText = $"{latestDay.Key:d}: {latestRow.RunningNetDailyMg:F0} net mg phosphorus, {latestRow.RunningDailyCalories} calories";

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
                    PillsTaken = month.Sum(row => row.PillsTaken)
                });
            }
        }

        public class FoodPhosphorusMonthlyTotal
        {
            public string Month { get; set; } = string.Empty;
            public int Entries { get; set; }
            public int RawPhosphorusMg { get; set; }
            public decimal NetPhosphorusMg { get; set; }
            public int Calories { get; set; }
            public int PillsTaken { get; set; }
        }
    }
}
