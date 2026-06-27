using System;

namespace DailyVitals.Domain.Models
{
    public class NutritionCoachStoredReview
    {
        public long NutritionCoachReviewId { get; set; }
        public long PersonId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string Model { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public NutritionCoachReview Review { get; set; } = new();
    }
}
