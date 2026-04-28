using DailyVitals.Data.Services.DailyVitals.App.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class ExerciseTrendViewModel : ViewModelBase
    {
        private readonly List<ExerciseSession> _trend;
        private ExerciseTrendMetricOption? _selectedMetric;
        private int _selectedMetricValueCount;

        public PointCollection TrendPoints { get; } = new();
        public ObservableCollection<ExerciseTrendMetricOption> MetricOptions { get; }

        public string PersonName { get; }
        public decimal StartValue { get; private set; }
        public decimal EndValue { get; private set; }
        public decimal NetChange => EndValue - StartValue;
        public string DateRange { get; private set; } = string.Empty;

        public ExerciseTrendMetricOption? SelectedMetric
        {
            get => _selectedMetric;
            set
            {
                if (_selectedMetric == value)
                    return;

                _selectedMetric = value;
                OnPropertyChanged();
                BuildTrend();
            }
        }

        public string TrendTitle =>
            SelectedMetric == null
                ? "Exercise Trend"
                : $"Exercise Trend ({SelectedMetric.DisplayName.ToLowerInvariant()} per session)";

        public string StartText =>
            SelectedMetric == null
                ? string.Empty
                : $"Start: {StartValue:F0} {SelectedMetric.Unit}";

        public string EndText =>
            SelectedMetric == null
                ? string.Empty
                : $"End: {EndValue:F0} {SelectedMetric.Unit}";

        public string TrendSummary
        {
            get
            {
                if (SelectedMetric == null)
                    return "No exercise history available";

                if (_selectedMetricValueCount < 2)
                    return $"Add another exercise session to compare {SelectedMetric.DisplayName.ToLowerInvariant()}";

                return NetChange > 0
                    ? $"Up {NetChange:F0} {SelectedMetric.Unit} from first to latest session"
                    : NetChange < 0
                        ? $"Down {Math.Abs(NetChange):F0} {SelectedMetric.Unit} from first to latest session"
                        : $"No {SelectedMetric.DisplayName.ToLowerInvariant()} change across the selected exercise history";
            }
        }

        public ExerciseTrendViewModel(long personId, string personName)
        {
            PersonName = personName;

            var service = new ExerciseService();
            _trend = service.GetHistory(personId)
                .OrderBy(session => session.StartTime)
                .TakeLast(30)
                .ToList();

            MetricOptions = new ObservableCollection<ExerciseTrendMetricOption>
            {
                new("Minutes", "min", session => session.DurationMinutes),
                new("Calories", "cal", session => session.CaloriesExpended)
            };

            SelectedMetric = MetricOptions.First();
        }

        private void BuildTrend()
        {
            TrendPoints.Clear();
            StartValue = 0;
            EndValue = 0;
            DateRange = string.Empty;
            _selectedMetricValueCount = 0;

            if (SelectedMetric == null)
            {
                NotifyTrendChanged();
                return;
            }

            var values = _trend
                .Select(session => new
                {
                    session.StartTime,
                    Value = SelectedMetric.Selector(session)
                })
                .Where(item => item.Value.HasValue)
                .Select(item => new
                {
                    item.StartTime,
                    Value = item.Value!.Value
                })
                .ToList();
            _selectedMetricValueCount = values.Count;

            if (values.Count >= 2)
            {
                StartValue = values.First().Value;
                EndValue = values.Last().Value;
                DateRange = $"{values.First().StartTime:d} - {values.Last().StartTime:d}";
            }
            else if (values.Count == 1)
            {
                StartValue = values[0].Value;
                EndValue = values[0].Value;
                DateRange = values[0].StartTime.ToShortDateString();
            }

            BuildPoints(values.Select(item => (double)item.Value).ToList());
            NotifyTrendChanged();
        }

        private void BuildPoints(List<double> values)
        {
            const double width = 520;
            const double height = 280;

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
        }

        private void NotifyTrendChanged()
        {
            OnPropertyChanged(nameof(TrendPoints));
            OnPropertyChanged(nameof(StartValue));
            OnPropertyChanged(nameof(EndValue));
            OnPropertyChanged(nameof(NetChange));
            OnPropertyChanged(nameof(DateRange));
            OnPropertyChanged(nameof(TrendTitle));
            OnPropertyChanged(nameof(StartText));
            OnPropertyChanged(nameof(EndText));
            OnPropertyChanged(nameof(TrendSummary));
        }

        public class ExerciseTrendMetricOption
        {
            public ExerciseTrendMetricOption(
                string displayName,
                string unit,
                Func<ExerciseSession, decimal?> selector)
            {
                DisplayName = displayName;
                Unit = unit;
                Selector = selector;
            }

            public string DisplayName { get; }
            public string Unit { get; }
            public Func<ExerciseSession, decimal?> Selector { get; }
        }
    }
}
