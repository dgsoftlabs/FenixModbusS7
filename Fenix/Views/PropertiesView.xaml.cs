using ProjectDataLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fenix
{
    public partial class PropertiesView : UserControl, INotifyPropertyChanged
    {
        private readonly ObservableCollection<PropertyRow> _rows = new ObservableCollection<PropertyRow>();
        private object _selectedObject;

        public PropertiesView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ObservableCollection<PropertyRow> Rows => _rows;

        public string SelectedObjectName => _selectedObject == null
            ? "No selection"
            : _selectedObject.GetType().Name;

        public bool IsEditorEnabled { get; private set; } = true;

        public object SelectedObject
        {
            get => _selectedObject;
            set
            {
                _selectedObject = value;
                RefreshRows();
                OnPropertyChanged(nameof(SelectedObjectName));
            }
        }

        public bool Enabled
        {
            get => IsEditorEnabled;
            set
            {
                if (IsEditorEnabled == value)
                    return;

                IsEditorEnabled = value;
                foreach (var row in _rows)
                {
                    row.UpdateEnabledState(value);
                }

                OnPropertyChanged(nameof(IsEditorEnabled));
            }
        }

        private void RefreshRows()
        {
            _rows.Clear();

            if (_selectedObject == null)
                return;

            var props = TypeDescriptor.GetProperties(_selectedObject)
                .Cast<PropertyDescriptor>()
                .Where(p => p.IsBrowsable)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayName);

            foreach (var prop in props)
            {
                if (!prop.Attributes.OfType<BlockRootNameAttribute>().Any())
                    _rows.Add(new PropertyRow(_selectedObject, prop, IsEditorEnabled));

                var nestedRows = GetExpandableRows(_selectedObject, prop);
                foreach (var nested in nestedRows)
                {
                    _rows.Add(nested);
                }
            }
        }

        private IEnumerable<PropertyRow> GetExpandableRows(object owner, PropertyDescriptor property)
        {
            object nestedObject;
            try
            {
                nestedObject = property.GetValue(owner);
            }
            catch
            {
                yield break;
            }

            if (nestedObject == null || nestedObject is string)
                yield break;

            var converter = property.Converter;
            var canExpand = converter != null;
            if (canExpand)
            {
                try
                {
                    canExpand = converter.GetPropertiesSupported();
                }
                catch
                {
                    canExpand = false;
                }
            }

            if (!canExpand)
                yield break;

            var nestedProps = TypeDescriptor.GetProperties(nestedObject)
                .Cast<PropertyDescriptor>()
                .Where(p => p.IsBrowsable)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayName)
                .ToList();

            foreach (var nestedProp in nestedProps)
            {
                yield return new PropertyRow(
                    nestedObject,
                    nestedProp,
                    IsEditorEnabled,
                    $"{property.DisplayName} / {nestedProp.DisplayName}");
            }
        }

        private void PropertyList_Loaded(object sender, RoutedEventArgs e)
        {
            if (PropertyList.View is GridView gridView)
            {
                var dpd = DependencyPropertyDescriptor.FromProperty(GridViewColumn.WidthProperty, typeof(GridViewColumn));
                // attach only to the "Property" column (all except last)
                for (var i = 0; i < gridView.Columns.Count - 1; i++)
                {
                    var col = gridView.Columns[i];
                    dpd.AddValueChanged(col, OnColumnWidthChanged);
                }
            }
            ScheduleAdjustColumns();
        }

        private void OnColumnWidthChanged(object sender, EventArgs e)
        {
            ScheduleAdjustColumns();
        }

        private void PropertyList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScheduleAdjustColumns();
        }

        private void ColorPreview_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is PropertyRow row)
            {
                row.SetColorFromPickerDialog();
            }
        }

        private void CollectionEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PropertyRow row)
            {
                row.EditCollection(Window.GetWindow(this));
            }
        }

        private void ScalingEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PropertyRow row)
            {
                row.EditScaling(Window.GetWindow(this));
            }
        }

        private void ScheduleAdjustColumns()
        {
            Dispatcher.BeginInvoke(new Action(AdjustColumns), DispatcherPriority.ContextIdle);
        }

        private void AdjustColumns()
        {
            if (PropertyList?.View is not GridView gridView || gridView.Columns.Count < 2)
                return;

            var totalWidth = GetListViewportWidth(PropertyList);
            if (totalWidth <= 0)
                return;

            var nonLastWidth = 0d;
            for (var i = 0; i < gridView.Columns.Count - 1; i++)
            {
                var col = gridView.Columns[i];
                nonLastWidth += col.ActualWidth > 0 ? col.ActualWidth : col.Width;
            }

            var lastColumn = gridView.Columns[gridView.Columns.Count - 1];
            var targetWidth = Math.Max(120, totalWidth - nonLastWidth - 2);
            if (!double.IsNaN(targetWidth) && !double.IsInfinity(targetWidth))
                lastColumn.Width = targetWidth;
        }

        private static double GetListViewportWidth(ListView listView)
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(listView);
            if (scrollViewer != null && scrollViewer.ViewportWidth > 0)
                return scrollViewer.ViewportWidth;

            var scrollBarWidth = scrollViewer?.ComputedVerticalScrollBarVisibility == Visibility.Visible
                ? SystemParameters.VerticalScrollBarWidth
                : 0;
            return listView.ActualWidth - listView.BorderThickness.Left - listView.BorderThickness.Right - scrollBarWidth;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    return typed;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public sealed class PropertyRow : INotifyPropertyChanged
        {
            private readonly object _target;
            private readonly PropertyDescriptor _property;
            private readonly string _nameOverride;
            private readonly Type _valueType;
            private readonly TypeConverter _converter;
            private readonly ITypeDescriptorContext _converterContext;
            private readonly bool _isStandardValuesExclusive;
            private readonly bool _useClearableTextEditor;
            private bool _isEnabled;

            private bool IsSupportedCollectionType =>
                _property.PropertyType == typeof(List<CustomTimer>) ||
                _property.PropertyType == typeof(List<UserClass>);

            public PropertyRow(object target, PropertyDescriptor property, bool enabled, string nameOverride = null)
            {
                _target = target;
                _property = property;
                _nameOverride = nameOverride;
                _valueType = Nullable.GetUnderlyingType(_property.PropertyType) ?? _property.PropertyType;
                _converter = _property.Converter;
                _converterContext = new PropertyTypeDescriptorContext(_target, _property);
                _isEnabled = enabled;
                _useClearableTextEditor = _property.Attributes[typeof(ClearableTextInputAttribute)] is ClearableTextInputAttribute;

                if (_valueType.IsEnum)
                    EnumValues = Enum.GetValues(_valueType).Cast<object>().ToList();

                if (_valueType != typeof(bool) && _converter != null)
                {
                    try
                    {
                        if (_converter.GetStandardValuesSupported(_converterContext))
                        {
                            var std = _converter.GetStandardValues(_converterContext);
                            if (std != null && std.Count > 0)
                                StandardValues = std.Cast<object>().ToList();

                            _isStandardValuesExclusive = _converter.GetStandardValuesExclusive(_converterContext);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            public string Name => string.IsNullOrWhiteSpace(_nameOverride) ? _property.DisplayName : _nameOverride;

            public string Category => string.IsNullOrWhiteSpace(_property.Category) ? "General" : _property.Category;

            public string Description => _property.Description;

            public bool IsReadOnly => _property.IsReadOnly;

            public bool IsEditable => _isEnabled && !IsReadOnly;

            public bool IsBoolEditorVisible => _valueType == typeof(bool);

            public bool IsEnumEditorVisible => _valueType.IsEnum && !IsStandardValuesEditorVisible;

            private bool IsColorType =>
                _valueType == typeof(System.Drawing.Color) ||
                _valueType.FullName == "System.Windows.Media.Color";

            public bool IsCollectionEditorVisible => IsSupportedCollectionType;

            public bool IsScalingEditorVisible => _valueType == typeof(ScalingConfig);

            public string ScalingSummary
            {
                get
                {
                    var v = _property.GetValue(_target) as ScalingConfig;
                    return v?.ToString() ?? "None";
                }
            }

            public string CollectionSummary
            {
                get
                {
                    if (_property.GetValue(_target) is not IList list)
                        return string.Empty;

                    return $"{list.Count} item(s)";
                }
            }

            public bool IsTextEditorVisible => !IsBoolEditorVisible && !IsEnumEditorVisible && !IsStandardValuesEditorVisible && !IsCollectionEditorVisible && !IsColorPickerVisible && !IsClearableTextEditorVisible && !IsScalingEditorVisible;

            public bool IsClearableTextEditorVisible => _useClearableTextEditor && !IsBoolEditorVisible && !IsEnumEditorVisible && !IsStandardValuesEditorVisible && !IsCollectionEditorVisible && !IsColorPickerVisible;

            public IList EnumValues { get; }

            public IList StandardValues { get; }

            public bool IsStandardValuesEditorVisible => StandardValues != null && _isStandardValuesExclusive && !IsColorType;

            public bool IsColorPreviewVisible => IsColorType;

            public bool IsColorPickerVisible => IsColorType;

            private Color GetWpfColor()
            {
                var value = _property.GetValue(_target);
                if (value is System.Drawing.Color dc)
                    return Color.FromArgb(dc.A, dc.R, dc.G, dc.B);
                if (value is Color mc)
                    return mc;
                return Colors.Black;
            }

            public Brush ColorPreviewBrush => new SolidColorBrush(GetWpfColor());

            public string ColorHexText
            {
                get
                {
                    var c = GetWpfColor();
                    return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                }
            }

            public Brush ColorForegroundBrush
            {
                get
                {
                    var c = GetWpfColor();
                    var luminance = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255;
                    return luminance > 0.5 ? Brushes.Black : Brushes.White;
                }
            }

            public object StandardValue
            {
                get
                {
                    var currentValue = _property.GetValue(_target);

                    if (currentValue != null && StandardValues != null && StandardValues.Count > 0)
                    {
                        var firstStandard = StandardValues.Cast<object>().FirstOrDefault();
                        if (firstStandard is string && _converter != null)
                        {
                            try
                            {
                                return _converter.ConvertTo(_converterContext, CultureInfo.InvariantCulture, currentValue, typeof(string)) as string ?? currentValue.ToString();
                            }
                            catch
                            {
                            }
                        }
                    }

                    return currentValue;
                }
                set
                {
                    if (!IsEditable)
                        return;

                    object parsed = value;

                    if (value != null && !_valueType.IsInstanceOfType(value))
                    {
                        if (_converter != null && _converter.CanConvertFrom(_converterContext, value.GetType()))
                        {
                            try
                            {
                                parsed = _converter.ConvertFrom(_converterContext, CultureInfo.InvariantCulture, value);
                            }
                            catch
                            {
                                if (_converter.CanConvertFrom(_converterContext, typeof(string)))
                                    parsed = _converter.ConvertFrom(_converterContext, CultureInfo.InvariantCulture, value.ToString());
                            }
                        }
                        else if (_converter != null && _converter.CanConvertFrom(_converterContext, typeof(string)))
                        {
                            parsed = _converter.ConvertFrom(_converterContext, CultureInfo.InvariantCulture, value.ToString());
                        }
                    }

                    _property.SetValue(_target, parsed);
                    OnPropertyChanged(nameof(StandardValue));
                    OnPropertyChanged(nameof(ValueText));
                    OnPropertyChanged(nameof(ColorPreviewBrush));
                }
            }

            public bool? BoolValue
            {
                get
                {
                    var value = _property.GetValue(_target);
                    return value as bool?;
                }
                set
                {
                    if (!IsEditable || value == null)
                        return;

                    _property.SetValue(_target, value.Value);
                    OnPropertyChanged(nameof(BoolValue));
                    OnPropertyChanged(nameof(ValueText));
                    OnPropertyChanged(nameof(ColorPreviewBrush));
                }
            }

            public object EnumValue
            {
                get => _property.GetValue(_target);
                set
                {
                    if (!IsEditable)
                        return;

                    _property.SetValue(_target, value);
                    OnPropertyChanged(nameof(EnumValue));
                    OnPropertyChanged(nameof(ValueText));
                    OnPropertyChanged(nameof(ColorPreviewBrush));
                }
            }

            public void SetColorFromPickerDialog()
            {
                var currentColor = _property.GetValue(_target);
                var wpfColor = currentColor is System.Drawing.Color drawingColor
                    ? Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B)
                    : (Color)currentColor;

                var dialog = new System.Windows.Forms.ColorDialog
                {
                    Color = System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B),
                    AllowFullOpen = true,
                    FullOpen = true
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var selectedColor = dialog.Color;

                    if (_valueType == typeof(System.Drawing.Color))
                    {
                        _property.SetValue(_target, selectedColor);
                    }
                    else if (_valueType.FullName == "System.Windows.Media.Color")
                    {
                        _property.SetValue(_target, Color.FromArgb(selectedColor.A, selectedColor.R, selectedColor.G, selectedColor.B));
                    }

                    OnPropertyChanged(nameof(ColorPreviewBrush));
                    OnPropertyChanged(nameof(ValueText));
                }
            }

            public string ValueText
            {
                get
                {
                    if (IsCollectionEditorVisible)
                        return CollectionSummary;

                    var value = _property.GetValue(_target);
                    if (value == null)
                        return string.Empty;

                    if (_valueType == typeof(string[]))
                    {
                        var values = value as string[];
                        if (values == null || values.Length == 0)
                            return string.Empty;

                        return string.Join("; ", values.Where(x => !string.IsNullOrWhiteSpace(x)));
                    }

                    if (value is double d && double.IsNaN(d))
                        return string.Empty;

                    if (value is float f && float.IsNaN(f))
                        return string.Empty;

                    if (_converter != null && _converter.CanConvertTo(_converterContext, typeof(string)))
                    {
                        try
                        {
                            return _converter.ConvertTo(_converterContext, CultureInfo.InvariantCulture, value, typeof(string)) as string ?? value.ToString();
                        }
                        catch
                        {
                        }
                    }

                    return value.ToString();
                }
                set
                {
                    if (IsCollectionEditorVisible)
                        return;

                    if (!IsEditable)
                        return;

                    try
                    {
                        object parsed;

                        if (_valueType == typeof(string))
                        {
                            parsed = value;
                        }
                        else if (_valueType == typeof(string[]))
                        {
                            parsed = string.IsNullOrWhiteSpace(value)
                                ? Array.Empty<string>()
                                : value
                                    .Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(x => x.Trim())
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ToArray();
                        }
                        else if (string.IsNullOrWhiteSpace(value) && _valueType == typeof(double))
                        {
                            parsed = double.NaN;
                        }
                        else if (string.IsNullOrWhiteSpace(value) && _valueType == typeof(float))
                        {
                            parsed = float.NaN;
                        }
                        else if (string.IsNullOrWhiteSpace(value) && Nullable.GetUnderlyingType(_property.PropertyType) != null)
                        {
                            parsed = null;
                        }
                        else if (_converter != null && _converter.CanConvertFrom(_converterContext, typeof(string)))
                        {
                            parsed = _converter.ConvertFrom(_converterContext, CultureInfo.InvariantCulture, value);
                        }
                        else
                        {
                            var converter = TypeDescriptor.GetConverter(_valueType);
                            parsed = converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
                        }

                        _property.SetValue(_target, parsed);
                        OnPropertyChanged(nameof(ValueText));
                        OnPropertyChanged(nameof(StandardValue));
                        OnPropertyChanged(nameof(ColorPreviewBrush));
                    }
                    catch
                    {
                    }
                }
            }

            public void EditCollection(Window owner)
            {
                if (!IsEditable)
                    return;

                if (_property.GetValue(_target) is not IList list)
                    return;

                if (_property.PropertyType == typeof(List<UserClass>))
                {
                    var dialog = new UsersEditorWindow(list.Cast<UserClass>())
                    {
                        Owner = owner
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        dialog.ApplyTo(list);
                        OnPropertyChanged(nameof(CollectionSummary));
                        OnPropertyChanged(nameof(ValueText));
                    }
                }
            }

            public void EditScaling(Window owner)
            {
                if (!IsEditable || _valueType != typeof(ScalingConfig))
                    return;

                var current = _property.GetValue(_target) as ScalingConfig ?? new ScalingConfig();
                var dialog = new ScalingEditorDialog(current) { Owner = owner };

                if (dialog.ShowDialog() == true)
                {
                    _property.SetValue(_target, dialog.Result);
                    OnPropertyChanged(nameof(ScalingSummary));
                    OnPropertyChanged(nameof(ValueText));
                }
            }

            public void UpdateEnabledState(bool enabled)
            {
                _isEnabled = enabled;
                OnPropertyChanged(nameof(IsEditable));
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private sealed class PropertyTypeDescriptorContext : ITypeDescriptorContext
            {
                private readonly object _instance;
                private readonly PropertyDescriptor _propertyDescriptor;

                public PropertyTypeDescriptorContext(object instance, PropertyDescriptor propertyDescriptor)
                {
                    _instance = instance;
                    _propertyDescriptor = propertyDescriptor;
                }

                public IContainer Container => null;

                public object Instance => _instance;

                public PropertyDescriptor PropertyDescriptor => _propertyDescriptor;

                public object GetService(Type serviceType)
                {
                    return null;
                }

                public bool OnComponentChanging()
                {
                    return false;
                }

                public void OnComponentChanged()
                {
                }
            }
        }
    }

    internal sealed class ScalingEditorDialog : Window
    {
        private readonly TextBox _appMinBox;
        private readonly TextBox _appMaxBox;
        private readonly TextBox _plcMinBox;
        private readonly TextBox _plcMaxBox;
        private readonly TextBlock _previewBlock;
        private readonly TextBlock _formulaReadBlock;
        private readonly TextBlock _formulaWriteBlock;

        public ScalingConfig Result { get; private set; }

        public ScalingEditorDialog(ScalingConfig current)
        {
            Title = "Scaling Editor";
            Width = 480;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ShowInTaskbar = false;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Headers
            var appHeader = new TextBlock { Text = "App Range", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) };
            Grid.SetRow(appHeader, 0); Grid.SetColumn(appHeader, 0); Grid.SetColumnSpan(appHeader, 2);
            grid.Children.Add(appHeader);

            var plcHeader = new TextBlock { Text = "PLC Range", FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 0, 6) };
            Grid.SetRow(plcHeader, 0); Grid.SetColumn(plcHeader, 2); Grid.SetColumnSpan(plcHeader, 2);
            grid.Children.Add(plcHeader);

            // Input fields
            _appMinBox = MakeField("Min", current.AppMin);
            _appMaxBox = MakeField("Max", current.AppMax);
            _plcMinBox = MakeField("Min", current.PlcMin);
            _plcMaxBox = MakeField("Max", current.PlcMax);

            var appPanel = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
            appPanel.Children.Add(MakeLabel("Min"));
            appPanel.Children.Add(_appMinBox);
            appPanel.Children.Add(MakeLabel("Max"));
            appPanel.Children.Add(_appMaxBox);
            Grid.SetRow(appPanel, 1); Grid.SetColumn(appPanel, 0); Grid.SetColumnSpan(appPanel, 2);
            grid.Children.Add(appPanel);

            var plcPanel = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
            plcPanel.Children.Add(MakeLabel("Min"));
            plcPanel.Children.Add(_plcMinBox);
            plcPanel.Children.Add(MakeLabel("Max"));
            plcPanel.Children.Add(_plcMaxBox);
            Grid.SetRow(plcPanel, 1); Grid.SetColumn(plcPanel, 2); Grid.SetColumnSpan(plcPanel, 2);
            grid.Children.Add(plcPanel);

            // Preview panel
            var previewBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 16, 0, 0)
            };
            previewBorder.SetResourceReference(Border.BorderBrushProperty, "Th.BorderBrush");
            previewBorder.SetResourceReference(Border.BackgroundProperty, "Th.SurfaceBrush");

            var previewGrid = new Grid();
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var previewTitle = new TextBlock { Text = "Live Preview", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(previewTitle, 0);
            previewGrid.Children.Add(previewTitle);

            _formulaReadBlock  = new TextBlock { FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
            _formulaWriteBlock = new TextBlock { FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
            _previewBlock      = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(_formulaReadBlock,  1); previewGrid.Children.Add(_formulaReadBlock);
            Grid.SetRow(_formulaWriteBlock, 2); previewGrid.Children.Add(_formulaWriteBlock);
            Grid.SetRow(_previewBlock,      3); previewGrid.Children.Add(_previewBlock);

            previewBorder.Child = previewGrid;
            Grid.SetRow(previewBorder, 2); Grid.SetColumnSpan(previewBorder, 4);
            grid.Children.Add(previewBorder);

            // Buttons
            var okButton = new Button { Content = "OK", Width = 90, Height = 28, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            okButton.Click += OkButton_Click;
            var cancelButton = new Button { Content = "Cancel", Width = 90, Height = 28, IsCancel = true };
            var clearButton  = new Button { Content = "Clear Scaling", Width = 110, Height = 28, HorizontalAlignment = HorizontalAlignment.Left };
            clearButton.Click += (_, _) => { _appMinBox.Text = _appMaxBox.Text = _plcMinBox.Text = _plcMaxBox.Text = "0"; UpdatePreview(); };

            var btnRow = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(clearButton,  0); btnRow.Children.Add(clearButton);
            Grid.SetColumn(okButton,     2); btnRow.Children.Add(okButton);
            Grid.SetColumn(cancelButton, 3); btnRow.Children.Add(cancelButton);
            Grid.SetRow(btnRow, 3); Grid.SetColumnSpan(btnRow, 4);
            grid.Children.Add(btnRow);

            Content = grid;

            foreach (var tb in new[] { _appMinBox, _appMaxBox, _plcMinBox, _plcMaxBox })
                tb.TextChanged += (_, _) => UpdatePreview();

            UpdatePreview();
        }

        private static TextBlock MakeLabel(string text) =>
            new TextBlock { Text = text, Margin = new Thickness(0, 8, 0, 2), FontSize = 11 };

        private static TextBox MakeField(string placeholder, double value) =>
            new TextBox
            {
                Text = value.ToString("G"),
                Height = 28,
                Padding = new Thickness(4, 4, 4, 4),
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            };

        private void UpdatePreview()
        {
            var cfg = BuildConfig();
            if (cfg.IsEnabled)
            {
                _formulaReadBlock.Text  = $"Read : plcValue → appValue = (plcValue - {cfg.PlcMin}) / ({cfg.PlcMax} - {cfg.PlcMin}) × ({cfg.AppMax} - {cfg.AppMin}) + {cfg.AppMin}";
                _formulaWriteBlock.Text = $"Write: appValue → plcValue = (appValue - {cfg.AppMin}) / ({cfg.AppMax} - {cfg.AppMin}) × ({cfg.PlcMax} - {cfg.PlcMin}) + {cfg.PlcMin}";
                _previewBlock.Text = $"Example:  PLC {cfg.PlcMin} → App {cfg.ToApp(cfg.PlcMin):G}   |   PLC {cfg.PlcMax} → App {cfg.ToApp(cfg.PlcMax):G}";
            }
            else
            {
                _formulaReadBlock.Text  = "";
                _formulaWriteBlock.Text = "";
                _previewBlock.Text = "No scaling — values passed through unchanged.";
            }
        }

        private ScalingConfig BuildConfig()
        {
            double.TryParse(_appMinBox?.Text, out double appMin);
            double.TryParse(_appMaxBox?.Text, out double appMax);
            double.TryParse(_plcMinBox?.Text, out double plcMin);
            double.TryParse(_plcMaxBox?.Text, out double plcMax);
            return new ScalingConfig { AppMin = appMin, AppMax = appMax, PlcMin = plcMin, PlcMax = plcMax };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = BuildConfig();
            DialogResult = true;
            Close();
        }
    }
}
