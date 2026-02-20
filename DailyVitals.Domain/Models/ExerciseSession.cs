using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Domain.Models
{
    public class ExerciseSession
    {
        public long ExerciseSessionId { get; set; }
        public long PersonId { get; set; }

        // ✅ REQUIRED
        public long ExerciseTypeId { get; set; }

        public string ExerciseName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public decimal DurationMinutes { get; set; }

        public string Intensity { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}
