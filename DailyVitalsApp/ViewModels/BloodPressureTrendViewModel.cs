using DailyVitals.App.Helper;
using DailyVitals.Data.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class BloodPressureTrendViewModel : ViewModelBase
    {
        private readonly BloodPressureService _bpService;

        public PointCollection SystolicPoints { get; } = new();
        public PointCollection DiastolicPoints { get; } = new();
        public ObservableCollection<BPLabel> SystolicLabels { get; } = new();
        public ObservableCollection<BPLabel> DiastolicLabels { get; } = new();




        public string HeaderText { get; }

        public BloodPressureTrendViewModel(long personId, string fullName)
        {
            _bpService = new BloodPressureService();

            HeaderText = $"Blood Pressure Trend - {fullName}";

            LoadData(personId);
        }

        private void LoadData(long personId)
        {
            var readings = _bpService
                .GetHistoryForPerson(personId)
                .OrderBy(r => r.ReadingTime)
                .ToList();

            if (!readings.Any())
                return;

            const double canvasHeight = 350;
            const double xSpacing = 60;
            const double leftPadding = 30;

            // Determine vertical scaling range
            int maxValue = readings.Max(r => r.Systolic);
            int minValue = readings.Min(r => r.Diastolic);

            double range = maxValue - minValue;

            if (range == 0)
                range = 1;

            double x = leftPadding;

            foreach (var r in readings)
            {
                double systolicY =
                    canvasHeight - ((r.Systolic - minValue) / range * canvasHeight);

                double diastolicY =
                    canvasHeight - ((r.Diastolic - minValue) / range * canvasHeight);

                SystolicPoints.Add(new Point(x, systolicY));
                SystolicLabels.Add(new BPLabel
                {
                    X = x,
                    Y = systolicY - 20, // offset above line
                    Value = r.Systolic.ToString()
                });
                DiastolicPoints.Add(new Point(x, diastolicY));
                DiastolicLabels.Add(new BPLabel
                {
                    X = x,
                    Y = diastolicY + 5, // slightly below
                    Value = r.Diastolic.ToString()
                });

                x += xSpacing;

            }

            MessageBox.Show($"Points: {SystolicPoints.Count}");

        }

    }
}
