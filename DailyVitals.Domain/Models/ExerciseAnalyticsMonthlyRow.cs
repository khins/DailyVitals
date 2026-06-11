using System;

namespace DailyVitals.Domain.Models
{
    public class ExerciseAnalyticsMonthlyRow
    {
        public DateTime Month { get; set; }
        public int Sessions { get; set; }
        public decimal TotalCalories { get; set; }
        public decimal AverageCalories { get; set; }
    }
}
