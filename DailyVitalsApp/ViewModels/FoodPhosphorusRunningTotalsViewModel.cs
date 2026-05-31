using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
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

        public string SummaryText { get; private set; } = "No food phosphorus entries available";

        private void Load(long personId)
        {
            Rows.Clear();

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
        }
    }
}
