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
    public partial class PropertiesGridManager : UserControl, INotifyPropertyChanged
    {
        private readonly ObservableCollection<PropertyRow> _rows = new ObservableCollection<PropertyRow>();
        private object _selectedObject;

        public PropertiesGridManager()
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

                if (!_valueType.IsEnum && _valueType != typeof(bool) && _converter != null)
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

            public bool IsEnumEditorVisible => _valueType.IsEnum;

            private bool IsColorType =>
                _valueType == typeof(System.Drawing.Color) ||
                _valueType.FullName == "System.Windows.Media.Color";

            public bool IsCollectionEditorVisible => _property.PropertyType == typeof(List<CustomTimer>);

            public string CollectionSummary
            {
                get
                {
                    if (_property.GetValue(_target) is not IList list)
                        return string.Empty;

                    return $"{list.Count} item(s)";
                }
            }

            public bool IsTextEditorVisible => !IsBoolEditorVisible && !IsEnumEditorVisible && !IsStandardValuesEditorVisible && !IsCollectionEditorVisible && !IsColorPickerVisible && !IsClearableTextEditorVisible;

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
                get => _property.GetValue(_target);
                set
                {
                    if (!IsEditable)
                        return;

                    _property.SetValue(_target, value);
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
                return;
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
}