namespace DailyVitals.Domain.Models
{
    public class RenalDietFood
    {
        public long RenalFoodId { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public string? ServingSize { get; set; }
        public int? Calories { get; set; }
        public int? SodiumMg { get; set; }
        public int? PotassiumMg { get; set; }
        public int? PhosphorusMg { get; set; }
        public decimal? ProteinG { get; set; }
        public string? CategoryName { get; set; }
        public string? RenalRating { get; set; }
        public string? GuidanceNotes { get; set; }
        public string? SourceNotes { get; set; }
        public bool IsActive { get; set; }

        public bool IsPreferred =>
            string.Equals(RenalRating, "Preferred", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(RenalRating, "Friendly", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(RenalRating, "Good", StringComparison.OrdinalIgnoreCase);
    }
}
