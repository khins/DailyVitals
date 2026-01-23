using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Domain.Models
{
    public class BloodGlucoseReading
    {
        public long GlucoseId { get; set; }
        public long GlucoseValue{ get; set; }
        public DateTime ReadingTime { get; set; }
        public string Notes { get; set; }
    }

}
