using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Domain.Models
{
    public class BloodPressureReading
    {
        public long BloodPressureId { get; set; }
        public int Systolic { get; set; }
        public int Diastolic { get; set; }
        public int Pulse { get; set; }
        public DateTime ReadingTime { get; set; }
        public string Notes { get; set; }
        public string DisplayValue => $"{Systolic} / {Diastolic}";
    }
}
