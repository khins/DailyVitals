using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Domain.Models
{
    public class Medication
    {
        public long MedicationId { get; set; }
        public long PersonId { get; set; }

        public string MedicationName { get; set; }
        public decimal DosageMg { get; set; }
        public string DosageForm { get; set; }

        public bool TakeMorning { get; set; }
        public bool TakeNoon { get; set; }
        public bool TakeEvening { get; set; }
        public bool TakeBedtime { get; set; }

        public string Instructions { get; set; }
        public string PrescribedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

}
