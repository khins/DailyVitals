namespace DailyVitals.Domain.Models
{
    public class FoodPhosphorusEstimate
    {
        public string FoodName { get; set; } = string.Empty;
        public string? ServingDescription { get; set; }
        public int EstimatedPhosphorusMg { get; set; }
        public int? EstimatedCalories { get; set; }
        public int? EstimatedSodiumMg { get; set; }
        public decimal? EstimatedProteinG { get; set; }
        public int? EstimatedPotassiumMg { get; set; }
        public string? Confidence { get; set; }
        public string? SourceNotes { get; set; }
    }
}
