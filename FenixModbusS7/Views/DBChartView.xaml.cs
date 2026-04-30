using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Threading.Tasks;
using System.IO;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for ChartViewDatabase.xaml
    /// </summary>
    public partial class DBChartView : UserControl, INotifyPropertyChanged
    {
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _selectedInterval;
        private string _interactionDescription;

        public event PropertyChangedEventHandler PropertyChanged;

        public PlotModel PlotModel { get; private set; }

        public PlotController PlotController { get; private set; }

        public LinearAxis AxY1 { get; set; }

        public LinearAxis AxY2 { get; set; }

        public DateTimeAxis AxX1 { get; set; }

        private Project _project;

        public ObservableCollection<string> TimeIntervals { get; set; }

        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate != value)
                {
                    _fromDate = value;
                    OnPropertyChanged();

                    if (_project != null && IsDateRangeEditable)
                    {
                        _ = RefreshChartAsync();
                    }
                }
            }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (_toDate != value)
                {
                    _toDate = value;
                    OnPropertyChanged();

                    if (_project != null && IsDateRangeEditable)
                    {
                        _ = RefreshChartAsync();
                    }
                }
            }
        }

        public bool IsDateRangeEditable => _selectedInterval == "Custom";

        public string SelectedInterval
        {
            get => _selectedInterval;
            set
            {
                if (_selectedInterval != value)
                {
                    _selectedInterval = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDateRangeEditable));
                    UpdateDateXRangeBasedOnInterval();

                    if (_project != null)
                    {
                        _ = RefreshChartAsync();
                    }
                }
            }
        }

        public string InteractionDescription
        {
            get => _interactionDescription;
            set
            {
                if (_interactionDescription != value)
                {
                    _interactionDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public async Task InitializeAsync(Project project)
        {
            _project = project;

            PlotController = new PlotController();
            InteractionDescription = "Pan:[ Left Click + Drag ] | Zoom Rectangle:[ Ctrl + Right Click + Drag ] | Zoom:[ Mouse Wheel ]";

            AxX1 = new DateTimeAxis { Position = AxisPosition.Bottom, MajorGridlineStyle = LineStyle.Dash, StringFormat = "HH:mm:ss" };

            PlotModel = new PlotModel();
            PlotModel.Axes.Add(AxX1);

            RebuildYAxes();

            ((INotifyPropertyChanged)_project.ChartConf).PropertyChanged += ChartConf_PropertyChanged;

            View.Model = PlotModel;

            await RefreshChartAsync();
        }

        private void RebuildYAxes()
        {
            if (_project?.ChartConf?.Axes != null)
                foreach (var axConf in _project.ChartConf.Axes)
                    ((INotifyPropertyChanged)axConf).PropertyChanged -= AxisConf_PropertyChanged;

            var toRemove = PlotModel.Axes.OfType<LinearAxis>().Where(a => a is not DateTimeAxis).ToList();
            foreach (var ax in toRemove)
                PlotModel.Axes.Remove(ax);

            AxY1 = null;
            AxY2 = null;

            var axisList = _project?.ChartConf?.Axes;
            if (axisList == null || axisList.Count == 0)
            {
                AxY1 = new LinearAxis { Key = "Y1", Position = AxisPosition.Left, MajorGridlineStyle = LineStyle.Dash };
                PlotModel.Axes.Add(AxY1);
                return;
            }

            int leftTier = 0, rightTier = 0;
            foreach (var axConf in axisList)
            {
                var position = axConf.IsRight ? AxisPosition.Right : AxisPosition.Left;
                int tier = axConf.IsRight ? rightTier++ : leftTier++;

                var ax = new LinearAxis
                {
                    Key = axConf.Key,
                    Title = axConf.Title,
                    Position = position,
                    PositionTier = tier,
                    MajorGridlineStyle = LineStyle.Dash,
                    IsAxisVisible = axConf.IsVisible,
                    Minimum = double.IsNaN(axConf.Minimum) ? double.NaN : axConf.Minimum,
                    Maximum = double.IsNaN(axConf.Maximum) ? double.NaN : axConf.Maximum
                };
                PlotModel.Axes.Add(ax);

                if (axConf.Key == "Y1") AxY1 = ax;
                if (axConf.Key == "Y2") AxY2 = ax;

                ((INotifyPropertyChanged)axConf).PropertyChanged += AxisConf_PropertyChanged;
            }

            if (AxY1 == null && PlotModel.Axes.OfType<LinearAxis>().Any())
                AxY1 = PlotModel.Axes.OfType<LinearAxis>().First();
        }

        private void AxisConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var axConf = (ChartAxisConf)sender;
            var ax = PlotModel.Axes.OfType<LinearAxis>().FirstOrDefault(a => a.Key == axConf.Key);
            if (ax == null) return;

            switch (e.PropertyName)
            {
                case nameof(ChartAxisConf.Minimum):   ax.Minimum = double.IsNaN(axConf.Minimum) ? double.NaN : axConf.Minimum; break;
                case nameof(ChartAxisConf.Maximum):   ax.Maximum = double.IsNaN(axConf.Maximum) ? double.NaN : axConf.Maximum; break;
                case nameof(ChartAxisConf.Title):     ax.Title = axConf.Title; break;
                case nameof(ChartAxisConf.IsVisible): ax.IsAxisVisible = axConf.IsVisible; break;
                case nameof(ChartAxisConf.IsRight):   ax.Position = axConf.IsRight ? AxisPosition.Right : AxisPosition.Left; break;
            }

            PlotModel.InvalidatePlot(false);
        }

        private void ChartConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChartViewConf.Axes))
            {
                Dispatcher.Invoke(() =>
                {
                    RebuildYAxes();
                    RevalidateSeriesAxisKeys();
                    PlotModel.InvalidatePlot(true);
                });
            }
        }

        private void RevalidateSeriesAxisKeys()
        {
            foreach (var series in PlotModel.Series.OfType<XYAxisSeries>())
            {
                if (!PlotModel.Axes.OfType<LinearAxis>().Any(a => a.Key == series.YAxisKey))
                    series.YAxisKey = "Y1";
            }
        }

        public DBChartView(Project project)
        {
            InitializeComponent();
            DataContext = this;

            TimeIntervals = new ObservableCollection<string> { "1h", "3h", "6h", "12h", "24h", "Custom" };
            SelectedInterval = TimeIntervals.First();

            Loaded += async (_, __) => await InitializeAsync(project);
            Unloaded += (_, __) =>
            {
                if (_project?.ChartConf != null)
                    ((INotifyPropertyChanged)_project.ChartConf).PropertyChanged -= ChartConf_PropertyChanged;
            };
        }

        private async Task RefreshChartAsync()
        {
            IsLoading = true;
            try
            {
                var data = await LoadDataFromDatabase(FromDate, ToDate);
                var series = await CreateSeriesAsync(data);
                AddSeries(series);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<List<TagDTO>> LoadDataFromDatabase(DateTime? from, DateTime? to)
        {
            return await Task.Run(() =>
            {
                var effectiveFrom = from ?? DateTime.MinValue;
                var effectiveTo = to ?? DateTime.MaxValue;
                var data = _project.Db.GetDataByStamp(effectiveFrom, effectiveTo) ?? new ObservableCollection<TagDTO>();
                return data.ToList();
            });
        }

        private async Task<List<LineSeries>> CreateSeriesAsync(List<TagDTO> tagGroups)
        {
            List<LineSeries> series = new List<LineSeries>();
            foreach (var group in tagGroups.GroupBy(x=>x.Name))
            {
                ITag tag = _project.GetITag(group.Key);
                if (tag is null) continue;

                var axisKey = string.IsNullOrEmpty(tag.GrAxisKey) ? "Y1" : tag.GrAxisKey;
                if (PlotModel.Axes.OfType<LinearAxis>().All(a => a.Key != axisKey))
                    axisKey = "Y1";

                var serie = new LineSeries
                {
                    Title = group.Key,
                    TrackerFormatString = "{0}" + Environment.NewLine + "Y: {4:0.000}" + Environment.NewLine + "X: {2:" + _project.longDT + "}",
                    Color = OxyColor.FromRgb(tag.Clr.R, tag.Clr.G, tag.Clr.B),
                    StrokeThickness = tag.Width,
                    IsVisible = true,
                    YAxisKey = axisKey
                };

                await Task.Run(() =>
                {
                    foreach (var point in group)
                    {
                        serie.Points.Add(new DataPoint(point.Stamp.ToOADate(), point.Value));
                    }
                });

                series.Add(serie);
            }

            return series;
        }

        private void AddSeries(List<LineSeries> series)
        {
            PlotModel.Series.Clear();

            foreach (var serie in series)
            {
                PlotModel.Series.Add(serie);
            }

            PlotModel.InvalidatePlot(true);

            PlotModel.ResetAllAxes();
        }

        private void UpdateDateXRangeBasedOnInterval()
        {
            if (_selectedInterval == "Custom")
                return;

            DateTime now = DateTime.Now;
            FromDate = SelectedInterval switch
            {
                "1h"  => now.AddHours(-1),
                "3h"  => now.AddHours(-3),
                "6h"  => now.AddHours(-6),
                "12h" => now.AddHours(-12),
                "24h" => now.AddHours(-24),
                _     => FromDate
            };
            ToDate = now;
        }

        private void Button_ExportPng_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"dbchart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };

                if (dlg.ShowDialog() == true)
                {
                    var oldBackground = PlotModel.Background;
                    var oldPlotAreaBackground = PlotModel.PlotAreaBackground;
                    try
                    {
                        PlotModel.Background = OxyColors.White;
                        PlotModel.PlotAreaBackground = OxyColors.White;

                        using var stream = File.Create(dlg.FileName);
                        var exporter = new PngExporter
                        {
                            Width = Math.Max(1, (int)View.ActualWidth),
                            Height = Math.Max(1, (int)View.ActualHeight)
                        };
                        exporter.Export(PlotModel, stream);
                    }
                    finally
                    {
                        PlotModel.Background = oldBackground;
                        PlotModel.PlotAreaBackground = oldPlotAreaBackground;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return !boolean;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return !boolean;
            }
            return false;
        }
    }
}