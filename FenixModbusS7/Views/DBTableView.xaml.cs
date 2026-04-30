using OxyPlot.Series;
using OxyPlot;
using ProjectDataLib;
using System.Data;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text;
using System.IO;

namespace Fenix
{
    public partial class DBTableView : UserControl, INotifyPropertyChanged
    {
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string _selectedInterval;
        private string _selectedOrder;
        private bool _isLoading;
        private readonly Project _project;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> TimeIntervals { get; }
        public ObservableCollection<string> OrderOptions { get; }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate != value)
                {
                    _fromDate = value;
                    OnPropertyChanged();
                    if (_selectedInterval == "Custom")
                        _ = RefreshDataAsync();
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
                    if (_selectedInterval == "Custom")
                        _ = RefreshDataAsync();
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
                    UpdateDateRange();
                    _ = RefreshDataAsync();
                }
            }
        }

        public string SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                if (_selectedOrder != value)
                {
                    _selectedOrder = value;
                    OnPropertyChanged();
                    _ = RefreshDataAsync();
                }
            }
        }

        public DBTableView(Project project)
        {
            InitializeComponent();
            DataContext = this;
            _project = project;

            // Initialize default values
            TimeIntervals = ["1h", "3h", "6h", "12h", "24h", "Custom"];
            OrderOptions = ["Descending", "Ascending"];
            SelectedInterval = TimeIntervals.First();
            SelectedOrder = OrderOptions.First();

            UpdateDateRange();
            GetDataFormDatabase();
        }

        private void UpdateDateRange()
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

        private async Task RefreshDataAsync()
        {
            IsLoading = true;
            await GetDataFormDatabase();
            IsLoading = false;
        }

        private async Task GetDataFormDatabase()
        {
            if (SelectedOrder == null || OrderOptions == null) return;

            bool descending = SelectedOrder == OrderOptions[0];
            var effectiveFrom = FromDate ?? DateTime.MinValue;
            var effectiveTo = ToDate ?? DateTime.MaxValue;
            var tags = await _project.Db.GetDataByStampAsync(effectiveFrom, effectiveTo, descending);
            myDataGrid.ItemsSource = BuildPivotTable(tags, descending).DefaultView;
        }

        private DataTable BuildPivotTable(System.Collections.Generic.List<TagDTO> tags, bool descending)
        {
            var table = new DataTable();
            var tagNames = tags.Select(t => t.Name).Distinct().ToList();

            table.Columns.Add("Stamp", typeof(string));
            foreach (var name in tagNames)
                table.Columns.Add(name, typeof(string));

            // Group by second-precision to merge tags from the same scan cycle
            var groups = tags.GroupBy(t => new DateTime(t.Stamp.Year, t.Stamp.Month, t.Stamp.Day,
                                                         t.Stamp.Hour, t.Stamp.Minute, t.Stamp.Second));
            var ordered = descending
                ? groups.OrderByDescending(g => g.Key)
                : groups.OrderBy(g => g.Key);

            foreach (var group in ordered)
            {
                var row = table.NewRow();
                row["Stamp"] = group.Key.ToString("yyyy-MM-dd HH:mm:ss");
                foreach (var tag in group)
                    row[tag.Name] = tag.Value.ToString();
                table.Rows.Add(row);
            }

            return table;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (myDataGrid.ItemsSource is not DataView dataView || dataView.Table == null)
                    return;

                var dlg = new SaveFileDialog
                {
                    Filter = "CSV File|*.csv",
                    FileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dlg.ShowDialog() != true)
                    return;

                var sb = new StringBuilder();
                var table = dataView.Table;

                // Header
                sb.AppendLine(string.Join(";", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));

                // Rows
                foreach (DataRowView row in dataView)
                    sb.AppendLine(string.Join(";", table.Columns.Cast<DataColumn>().Select(c => row[c.ColumnName]?.ToString() ?? "")));

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}