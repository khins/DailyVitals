using DailyVitals.Data.Services.DailyVitals.App.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class ExerciseTrendViewModel : ViewModelBase
    {
        public PointCollection TrendPoints { get; } = new();

        public string PersonName { get; }
        public decimal StartMinutes { get; }
        public decimal EndMinutes { get; }
        public decimal NetChange => EndMinutes - StartMinutes;
        public string DateRange { get; } = string.Empty;

        public string TrendSummary =>
            NetChange > 0
                ? $"↑ Up {NetChange:F0} min from first to latest session"
                : NetChange < 0
                    ? $"↓ Down {Math.Abs(NetChange):F0} min from first to latest session"
                    : "No duration change across the selected exercise history";

        public ExerciseTrendViewModel(long personId, string personName)
        {
            PersonName = personName;

            var service = new ExerciseService();
            var trend = service.GetHistory(personId)
                .OrderBy(session => session.StartTime)
                .TakeLast(30)
                .ToList();

            if (trend.Count >= 2)
            {
                StartMinutes = trend.First().DurationMinutes;
                EndMinutes = trend.Last().DurationMinutes;
                DateRange = $"{trend.First().StartTime:d} – {trend.Last().StartTime:d}";
            }
            else if (trend.Count == 1)
            {
                StartMinutes = trend[0].DurationMinutes;
                EndMinutes = trend[0].DurationMinutes;
                DateRange = trend[0].StartTime.ToShortDateString();
            }

            BuildPoints(trend);
        }

        private void BuildPoints(List<ExerciseSession> trend)
        {
            const double width = 520;
            const double height = 280;

            var values = trend.Select(session => (double)session.DurationMinutes).ToList();
            if (values.Count < 2)
                return;

            double min = values.Min();
            double max = values.Max();
            double range = Math.Max(max - min, 1);

            for (int i = 0; i < values.Count; i++)
            {
                double x = i * (width / (values.Count - 1));
                double y = height - ((values[i] - min) / range * height);
                TrendPoints.Add(new Point(x, y));
            }

            OnPropertyChanged(nameof(TrendPoints));
        }
    }
}
