using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Domain.Models
{
    public class WeightReading
    {
        public long WeightId { get; set; }
        public decimal WeightValue { get; set; }
        public string WeightUnit { get; set; }
        public DateTime ReadingTime { get; set; }
        public string Notes { get; set; }
        public decimal? HeightFt { get; set; }

    }

}
