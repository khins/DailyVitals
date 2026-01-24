using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Domain.Models.Calculations
{
    public static class HealthMetrics
    {
        /// <summary>
        /// Calculates BMI using weight in pounds and height in feet (decimal).
        /// </summary>
        public static decimal? CalculateBMI(decimal weightLb, decimal heightFt)
        {
            if (weightLb <= 0 || heightFt <= 0)
                return null;

            var weightKg = weightLb * 0.453592m;
            var heightM = heightFt * 0.3048m;

            if (heightM <= 0)
                return null;

            return Math.Round(weightKg / (heightM * heightM), 1);
        }
    }
}
