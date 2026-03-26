namespace DailyVitals.Domain.Models
{
    public class RenalDietFood
    {
        public long RenalFoodId { get; set; }
        public long PersonId { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public string? ServingSize { get; set; }
        public int? Calories { get; set; }
        public int? SodiumMg { get; set; }
        public int? PotassiumMg { get; set; }
        public int? PhosphorusMg { get; set; }
        public decimal? ProteinG { get; set; }
        public bool Allowed { get; set; }
        public string? RestrictionNotes { get; set; }
        public string? CategoryName { get; set; }
    }
}
