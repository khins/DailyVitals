using System;

namespace DailyVitals.Domain.Models
{
    public class KidneyLabResult
    {
        public long KidneyLabResultId { get; set; }
        public long PersonId { get; set; }
        public DateTime ResultMonth { get; set; }
        public decimal Albumin { get; set; }
        public decimal NPCR { get; set; }
        public decimal Potassium { get; set; }
        public decimal WKtV { get; set; }
        public decimal Calcium { get; set; }
        public decimal Phosphorus { get; set; }
        public decimal IPTH { get; set; }
        public decimal Hemoglobin { get; set; }
        public decimal Glucose { get; set; }
        public decimal Cholesterol { get; set; }
        public decimal Triglycerides { get; set; }
        public decimal BUN { get; set; }
        public decimal Creatinine { get; set; }
        public string? Notes { get; set; }

        public string DisplayMonth => ResultMonth.ToString("MMM yyyy");
    }
}
