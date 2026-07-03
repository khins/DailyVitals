using System;

namespace DailyVitals.Domain.Models
{
    public class FluidIntake
    {
        public long FluidIntakeId { get; set; }
        public long PersonId { get; set; }
        public DateTime ConsumedAt { get; set; }
        public int FluidMl { get; set; }
        public decimal EnteredAmount { get; set; }
        public string EnteredUnit { get; set; } = "mL";
        public string BeverageName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
