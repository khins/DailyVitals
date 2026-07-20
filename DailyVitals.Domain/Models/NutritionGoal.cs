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
        public bool PhosphorusEnabled { get; set; } = true;
        public bool SodiumEnabled { get; set; } = true;
        public bool CalorieEnabled { get; set; } = true;
        public bool ProteinEnabled { get; set; } = true;
        public bool PotassiumEnabled { get; set; } = true;
        public bool FluidEnabled { get; set; } = true;
    }
}
