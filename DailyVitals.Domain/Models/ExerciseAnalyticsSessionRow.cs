using System;

namespace DailyVitals.Domain.Models
{
    public class ExerciseAnalyticsSessionRow
    {
        public DateTime StartTime { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public decimal DurationMinutes { get; set; }
        public decimal CaloriesExpended { get; set; }
    }
}
