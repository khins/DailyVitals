using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Domain.Models
{
    public class ExerciseType
    {
        public long ExerciseTypeId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
    }
}
