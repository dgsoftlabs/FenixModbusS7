using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Threading.Tasks;

namespace FenixWPF
{
    /// <summary>
    /// Interaction logic for ChartViewDatabase.xaml
    /// </summary>
    public partial class ChartViewDatabase : UserControl, INotifyPropertyChanged
    {
        private DateTime _fromDate;
        private DateTime _toDate;
        private string _selectedInterval;
        private string _interactionDescription;
        private double _yAxisMinimum;
        private double _yAxisMaximum;

        public event PropertyChangedEventHandler PropertyChanged;

        public PlotModel PlotModel { get; private set; }

        public PlotController PlotController { get; private set; }

        public LinearAxis AxY1 { get; set; }

        public DateTimeAxis AxX1 { get; set; }

        private Project _project;

        public ObservableCollection<string> TimeIntervals { get; set; }

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate != value)
                {
                    _fromDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (_toDate != value)
                {
                    _toDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedInterval
        {
            get => _selectedInterval;
            set
            {
                if (_selectedInterval != value)
                {
                    _selectedInterval = value;
                    OnPropertyChanged();
                    UpdateDateXRangeBasedOnInterval();
                }
            }
        }

        public double YAxisMinimum
        {
            get => _yAxisMinimum;
            set
            {
                if (_yAxisMinimum != value)
                {
                    _yAxisMinimum = value;
                    OnPropertyChanged();
                    UpdatePlotYAxes();
                }
            }
        }

        public double YAxisMaximum
        {
            get => _yAxisMaximum;
            set
            {
                if (_yAxisMaximum != value)
                {
                    _yAxisMaximum = value;
                    OnPropertyChanged();
                    UpdatePlotYAxes();
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
            InitializeComponent();
            DataContext = this;
            _project = project;

            // Initialize PlotController
            PlotController = new PlotController();

            // Set interaction descriptions
            InteractionDescription = "Left Click + Drag: Pan | Ctrl + Right Click + Drag: Zoom Rectangle | Mouse Wheel: Zoom | C: Copy to Clipboard";

            // Axes
            AxY1 = new LinearAxis { Position = AxisPosition.Left, MajorGridlineStyle = LineStyle.Dash };
            AxX1 = new DateTimeAxis { Position = AxisPosition.Bottom, MajorGridlineStyle = LineStyle.Dash, StringFormat = "HH:mm:ss" };

            // Plot
            PlotModel = new PlotModel();
            PlotModel.Axes.Add(AxX1);
            PlotModel.Axes.Add(AxY1);
            View.Model = PlotModel;

            var data = await LoadDataFromDatabase(DateTime.Now.AddHours(-1), DateTime.Now, false);
            await CreateSeriesAsync(data).ContinueWith(task => AddSeries(task.Result));
            if (data.Count != 0)
            {
                YAxisMinimum = Math.Floor(data.Min(x => x.Value));
                YAxisMaximum = Math.Floor(data.Max(x => x.Value));
            }
            else
            {
                YAxisMinimum = 0;
                YAxisMaximum = 100;
            }
        }

        public ChartViewDatabase(Project project)
        {
            InitializeAsync(project);

            // Initialize default values
            TimeIntervals = new ObservableCollection<string> { "1h", "3h", "6h", "12h", "24h" };
            SelectedInterval = TimeIntervals.First();
            UpdatePlotYAxes();
        }

        private async Task<List<TagDTO>> LoadDataFromDatabase(DateTime from, DateTime to, bool blockFilters)
        {
            return await Task.Run(() =>
            {
                ObservableCollection<TagDTO> data = new ObservableCollection<TagDTO>();
                if (blockFilters)
                {
                    data = _project.Db.GetAllObservableCollection() ?? new ObservableCollection<TagDTO>();
                }
                else
                {
                    data = _project.Db.GetDataByStamp(from, to) ?? new ObservableCollection<TagDTO>();
                }
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

                var serie = new LineSeries
                {
                    Title = group.Key,
                    TrackerFormatString = "{0}" + Environment.NewLine + "Y: {4:0.000}" + Environment.NewLine + "X: {2:" + _project.longDT + "}",
                    Color = OxyColor.FromRgb(tag.Clr.R, tag.Clr.G, tag.Clr.B),
                    StrokeThickness = tag.Width,
                    IsVisible = true
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
            DateTime now = DateTime.Now;
            FromDate = SelectedInterval switch
            {
                "1h" => now.AddHours(-1),
                "3h" => now.AddHours(-3),
                "6h" => now.AddHours(-6),
                "12h" => now.AddHours(-12),
                "24h" => now.AddHours(-24),
                _ => FromDate
            };
            ToDate = now;
        }

        private void UpdatePlotYAxes()
        {
            if (AxY1 != null)
            {
                AxY1.Minimum = YAxisMinimum;
                AxY1.Maximum = YAxisMaximum;
            }

            PlotModel.InvalidatePlot(false);
        }

        private async void ShowAllButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IsLoading = true;
            if (DisableFormCheckBox.IsChecked is true)
            {             
                var data = await LoadDataFromDatabase(FromDate, ToDate, true);
                await CreateSeriesAsync(data).ContinueWith(task => AddSeries(task.Result));
            }
            else
            {
                var data = await LoadDataFromDatabase(FromDate, ToDate, false);
                await CreateSeriesAsync(data).ContinueWith(task => AddSeries(task.Result));
            }
            IsLoading = false;
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