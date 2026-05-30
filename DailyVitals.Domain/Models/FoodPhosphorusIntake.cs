using System;

namespace DailyVitals.Domain.Models
{
    public class FoodPhosphorusIntake
    {
        public long FoodPhosphorusIntakeId { get; set; }
        public long PersonId { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public int PhosphorusMg { get; set; }
        public int? Calories { get; set; }
        public int Binders { get; set; }
        public DateTime ConsumedAt { get; set; }
        public string? Notes { get; set; }
        public string? ServingDescription { get; set; }
        public bool EstimatedByAi { get; set; }
        public string? AiProvider { get; set; }
        public string? AiConfidence { get; set; }
        public string? SourceNotes { get; set; }
    }
}
