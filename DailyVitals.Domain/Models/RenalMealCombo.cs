using System.Collections.Generic;

namespace DailyVitals.Domain.Models
{
    public class RenalMealCombo
    {
        public IReadOnlyList<RenalDietFood> Foods { get; init; } = new List<RenalDietFood>();
        public string MealStyle { get; init; } = string.Empty;
        public decimal TotalProteinG { get; init; }
        public int TotalCalories { get; init; }
        public int TotalSodiumMg { get; init; }
        public int TotalPotassiumMg { get; init; }
        public int TotalPhosphorusMg { get; init; }
    }
}
