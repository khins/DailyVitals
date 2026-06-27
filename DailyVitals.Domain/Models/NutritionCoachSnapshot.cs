using System;
using System.Collections.Generic;

namespace DailyVitals.Domain.Models
{
    public class NutritionCoachSnapshot
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int DaysInPeriod { get; set; }
        public int DaysLogged { get; set; }
        public int FoodEntries { get; set; }
        public int BindersLogged { get; set; }
        public NutritionCoachMetric Sodium { get; set; } = new();
        public NutritionCoachMetric Phosphorus { get; set; } = new();
        public NutritionCoachMetric Protein { get; set; } = new();
        public NutritionCoachMetric Potassium { get; set; } = new();
        public List<NutritionCoachSource> TopSodiumSources { get; set; } = [];
        public List<NutritionCoachSource> TopPhosphorusSources { get; set; } = [];
        public List<NutritionCoachSource> TopProteinSources { get; set; } = [];
        public List<NutritionCoachSource> TopPotassiumSources { get; set; } = [];
    }

    public class NutritionCoachMetric
    {
        public decimal Goal { get; set; }
        public decimal AverageOnLoggedDays { get; set; }
        public int DaysMeetingGoal { get; set; }
        public string GoalType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public class NutritionCoachSource
    {
        public string FoodName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}
