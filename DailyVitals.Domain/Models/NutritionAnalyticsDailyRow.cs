using System;

namespace DailyVitals.Domain.Models
{
    public class NutritionAnalyticsDailyRow
    {
        public DateTime Date { get; set; }
        public int CaloriesIn { get; set; }
        public int ExerciseCalories { get; set; }
        public int CalorieBalance { get; set; }
        public int SodiumMg { get; set; }
        public int PhosphorusMg { get; set; }
        public decimal NetPhosphorusMg { get; set; }
        public decimal? WeightValue { get; set; }
        public int? CalorieLimit { get; set; }
        public int? SodiumLimitMg { get; set; }
        public int? PhosphorusLimitMg { get; set; }
    }
}
