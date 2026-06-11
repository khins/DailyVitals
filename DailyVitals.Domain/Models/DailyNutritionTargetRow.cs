namespace DailyVitals.Domain.Models
{
    public class DailyNutritionTargetRow
    {
        public string Nutrient { get; set; } = string.Empty;
        public string Consumed { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Remaining { get; set; } = string.Empty;
        public bool IsOverTarget { get; set; }
    }
}
