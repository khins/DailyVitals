using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class WeightTrendViewModel : ViewModelBase
    {
        public PointCollection TrendPoints { get; } = new();

        public string PersonName { get; }
        public decimal StartWeight { get; }
        public decimal EndWeight { get; }
        public decimal NetChange => EndWeight - StartWeight;

        public string DateRange { get; }
        public string TrendSummary =>
            NetChange > 0
                ? $"↑ Gained {NetChange:F1} lb"
                : NetChange < 0
                    ? $"↓ Lost {Math.Abs(NetChange):F1} lb"
                    : "No change";


        public WeightTrendViewModel(long personId, string personName)
        {
            PersonName = personName;

            var service = new WeightService();
            var trend = service.GetWeightTrend(personId, 30);

            if (trend.Count >= 2)
            {
                StartWeight = trend.First().Value;
                EndWeight = trend.Last().Value;

                DateRange =
                    $"{trend.First().Date:d} – {trend.Last().Date:d}";
            }

            BuildPoints(trend);
        }


        private void BuildPoints(List<TrendPoint> trend)
        {
            double width = 520;
            double height = 280;

            var values = trend.Select(t => (double)t.Value).ToList();
            if (values.Count < 2) return;

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
