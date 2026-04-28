using DailyVitals.App.Commands;
using DailyVitals.App.Helper;
using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class KidneyLabTrendViewModel : ViewModelBase
    {
        private const double ChartWidth = 760;
        private const double ChartHeight = 320;

        private readonly KidneyLabResultService _service = new();
        private readonly long _personId;
        private readonly List<KidneyLabMetricOption> _metricOptions;

        private KidneyLabMetricOption? _selectedMetric;
        private string _trendSummary = "No lab history available.";
        private string _dateRange = string.Empty;

        public KidneyLabTrendViewModel(long personId, string personName)
        {
            _personId = personId;
            PersonName = personName;
            History = _service
                .GetHistory(personId)
                .OrderBy(item => item.ResultMonth)
                .ToList();

            _metricOptions = BuildMetricOptions();
            MetricOptions = new ObservableCollection<KidneyLabMetricOption>(_metricOptions);
            RefreshCommand = new RelayCommand(BuildTrend, () => SelectedMetric != null);

            if (MetricOptions.Count > 0)
                SelectedMetric = MetricOptions.First();
            else
                BuildTrend();
        }

        public string PersonName { get; }
        public string HeaderText => $"Monthly Kidney Lab Trends - {PersonName}";
        public string DateRange
        {
            get => _dateRange;
            private set
            {
                _dateRange = value;
                OnPropertyChanged();
            }
        }

        public string TrendSummary
        {
            get => _trendSummary;
            private set
            {
                _trendSummary = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<KidneyLabMetricOption> MetricOptions { get; }
        public PointCollection TrendPoints { get; } = new();
        public ObservableCollection<ChartLabel> ValueLabels { get; } = new();
        public ObservableCollection<ChartLabel> MonthLabels { get; } = new();
        public ICommand RefreshCommand { get; }
        public IReadOnlyList<KidneyLabResult> History { get; }

        public KidneyLabMetricOption? SelectedMetric
        {
            get => _selectedMetric;
            set
            {
                if (_selectedMetric == value)
                    return;

                _selectedMetric = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMetricName));
                BuildTrend();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedMetricName => SelectedMetric?.DisplayName ?? "Metric";

        private void BuildTrend()
        {
            TrendPoints.Clear();
            ValueLabels.Clear();
            MonthLabels.Clear();

            if (SelectedMetric == null || History.Count == 0)
            {
                DateRange = string.Empty;
                TrendSummary = "No lab history available.";
                OnPropertyChanged(nameof(TrendPoints));
                return;
            }

            var values = History
                .Select(item => new
                {
                    item.ResultMonth,
                    Value = SelectedMetric.Selector(item)
                })
                .ToList();

            if (values.Count == 0)
            {
                DateRange = string.Empty;
                TrendSummary = $"No {SelectedMetric.DisplayName} values available.";
                OnPropertyChanged(nameof(TrendPoints));
                return;
            }

            DateRange = $"{values.First().ResultMonth:MMM yyyy} - {values.Last().ResultMonth:MMM yyyy}";

            double minValue = values.Min(item => (double)item.Value);
            double maxValue = values.Max(item => (double)item.Value);
            double range = Math.Max(maxValue - minValue, 1);
            double spacing = values.Count == 1 ? ChartWidth / 2d : ChartWidth / (values.Count - 1d);

            for (int i = 0; i < values.Count; i++)
            {
                double x = values.Count == 1 ? ChartWidth / 2d : i * spacing;
                double y = ChartHeight - ((((double)values[i].Value - minValue) / range) * ChartHeight);

                TrendPoints.Add(new Point(x, y));
                ValueLabels.Add(new ChartLabel
                {
                    X = Math.Max(0, x - 14),
                    Y = Math.Max(0, y - 22),
                    Value = values[i].Value.ToString("0.##")
                });
                MonthLabels.Add(new ChartLabel
                {
                    X = Math.Max(0, x - 22),
                    Y = ChartHeight + 8,
                    Value = values[i].ResultMonth.ToString("MMM yy")
                });
            }

            var first = values.First();
            var last = values.Last();
            var delta = last.Value - first.Value;
            var direction = delta > 0 ? "up" : delta < 0 ? "down" : "flat";

            TrendSummary =
                $"{SelectedMetric.DisplayName}: {first.Value:0.##} to {last.Value:0.##} ({direction}, {Math.Abs(delta):0.##})";

            OnPropertyChanged(nameof(TrendPoints));
        }

        private static List<KidneyLabMetricOption> BuildMetricOptions() =>
            new()
            {
                new("Albumin", item => item.Albumin),
                new("nPCR", item => item.NPCR),
                new("Potassium", item => item.Potassium),
                new("wKt/V", item => item.WKtV),
                new("Calcium", item => item.Calcium),
                new("Phosphorus", item => item.Phosphorus),
                new("iPTH", item => item.IPTH),
                new("Hemoglobin", item => item.Hemoglobin),
                new("Glucose", item => item.Glucose),
                new("Cholesterol", item => item.Cholesterol),
                new("Triglycerides", item => item.Triglycerides),
                new("BUN", item => item.BUN),
                new("Creatinine", item => item.Creatinine)
            };

        public class KidneyLabMetricOption
        {
            public KidneyLabMetricOption(string displayName, Func<KidneyLabResult, decimal> selector)
            {
                DisplayName = displayName;
                Selector = selector;
            }

            public string DisplayName { get; }
            public Func<KidneyLabResult, decimal> Selector { get; }
        }
    }
}
