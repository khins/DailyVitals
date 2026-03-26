using System;

namespace DailyVitals.Domain.Models.Calculations
{
    public static class ExerciseMetrics
    {
        public static decimal? EstimateCaloriesBurned(
            decimal durationMinutes,
            string intensity,
            decimal bodyWeight,
            string weightUnit)
        {
            if (durationMinutes <= 0 || bodyWeight <= 0)
                return null;

            var met = intensity?.Trim().ToLowerInvariant() switch
            {
                "low" => 3.5m,
                "moderate" => 5.0m,
                "high" => 7.0m,
                _ => 4.0m
            };

            var weightKg = NormalizeWeightToKg(bodyWeight, weightUnit);
            var calories = met * 3.5m * weightKg / 200m * durationMinutes;

            return Math.Round(calories, 0);
        }

        private static decimal NormalizeWeightToKg(decimal bodyWeight, string weightUnit)
        {
            return weightUnit?.Trim().ToLowerInvariant() switch
            {
                "kg" => bodyWeight,
                _ => bodyWeight * 0.453592m
            };
        }
    }
}
