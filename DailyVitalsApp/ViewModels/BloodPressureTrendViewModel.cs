using DailyVitals.App.Commands;
using DailyVitals.App.Helper;
using DailyVitals.Data.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DailyVitals.App.ViewModels
{
    public class BloodPressureTrendViewModel : ViewModelBase
    {
        private readonly BloodPressureService _bpService;
        private readonly long _personId;
        public PointCollection SystolicPoints { get; } = new();
        public PointCollection DiastolicPoints { get; } = new();
        public ObservableCollection<BPLabel> SystolicLabels { get; } = new();
        public ObservableCollection<BPLabel> DiastolicLabels { get; } = new();


        private DateTime _startDate;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged(nameof(StartDate));
                }
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged(nameof(EndDate));
                }
            }
        }
        public ICommand RefreshCommand { get; }

        private void LoadTrend(long personId)
        {   
            // 1️⃣ Clear old render data
            SystolicPoints.Clear();
            DiastolicPoints.Clear();
            SystolicLabels.Clear();
            DiastolicLabels.Clear();

            var readings = _bpService.GetTrend(personId, StartDate, EndDate);

            if (!readings.Any())
                return;

            double canvasWidth = 750;   // match XAML canvas width
            double canvasHeight = 350;

            double spacing = canvasWidth / Math.Max(readings.Count - 1, 1);

            double minValue = readings.Min(r => Math.Min(r.Systolic, r.Diastolic)) - 5;
            double maxValue = readings.Max(r => Math.Max(r.Systolic, r.Diastolic)) + 5;

            double range = maxValue - minValue;

            double x = 0;

            foreach (var r in readings)
            {
                double systolicY = canvasHeight - ((r.Systolic - minValue) / range) * canvasHeight;
                double diastolicY = canvasHeight - ((r.Diastolic - minValue) / range) * canvasHeight;

                SystolicPoints.Add(new Point(x, systolicY));
                DiastolicPoints.Add(new Point(x, diastolicY));

                SystolicLabels.Add(new BPLabel
                {
                    X = x,
                    Y = systolicY - 18,
                    Value = r.Systolic.ToString()
                });

                DiastolicLabels.Add(new BPLabel
                {
                    X = x,
                    Y = diastolicY - 18,
                    Value = r.Diastolic.ToString()
                });

                x += spacing;
            }

            OnPropertyChanged(nameof(SystolicPoints));
            OnPropertyChanged(nameof(DiastolicPoints));
        }

        public string HeaderText { get; }

        public BloodPressureTrendViewModel(long personId, string fullName)
        {
            _personId = personId;     // make sure this exists
            _bpService = new BloodPressureService();

            HeaderText = $"Blood Pressure Trend - {fullName}";

            EndDate = DateTime.Today;
            StartDate = EndDate.AddDays(-13);

            RefreshCommand = new RelayCommand(() => LoadTrend(_personId));

            LoadTrend(_personId);
        }

        private void LoadData()
        {
            var readings = _bpService
                .GetHistoryForPerson(_personId)
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

            //MessageBox.Show($"Points: {SystolicPoints.Count}");

        }

    }
}
