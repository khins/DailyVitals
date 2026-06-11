using System;

namespace DailyVitals.Domain.Models
{
    public class NutritionGoal
    {
        public long NutritionGoalId { get; set; }
        public long PersonId { get; set; }
        public int SodiumLimitMg { get; set; }
        public int PhosphorusLimitMg { get; set; }
        public int CalorieLimit { get; set; }
        public DateTime EffectiveDate { get; set; }
        public int? ProteinTargetG { get; set; }
        public int? PotassiumLimitMg { get; set; }
        public int? FluidLimitMl { get; set; }
    }
}
