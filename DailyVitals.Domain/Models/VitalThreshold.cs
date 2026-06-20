using System;

namespace DailyVitals.Domain.Models
{
    public class VitalThreshold
    {
        public long ThresholdId { get; set; }
        public string VitalType { get; set; } = string.Empty;
        public long? PersonId { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string Severity { get; set; } = "medium";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
