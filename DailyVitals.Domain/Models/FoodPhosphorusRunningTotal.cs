using System;

namespace DailyVitals.Domain.Models
{
    public class FoodPhosphorusRunningTotal
    {
        public DateTime IntakeDate { get; set; }
        public DateTime ConsumedAt { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public int RawPhosphorusMg { get; set; }
        public int Calories { get; set; }
        public int PillsTaken { get; set; }
        public decimal NetItemPhosphorusMg { get; set; }
        public decimal RunningNetDailyMg { get; set; }
        public long RunningDailyCalories { get; set; }
    }
}
