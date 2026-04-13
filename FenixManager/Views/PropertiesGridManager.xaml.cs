using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace FenixWPF
{
    public partial class PropertiesGridManager : UserControl
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
            }
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
            private readonly Type _valueType;
            private readonly TypeConverter _converter;
            private readonly ITypeDescriptorContext _converterContext;
            private readonly bool _isStandardValuesExclusive;
            private bool _isEnabled;

            public PropertyRow(object target, PropertyDescriptor property, bool enabled)
            {
                _target = target;
                _property = property;
                _valueType = Nullable.GetUnderlyingType(_property.PropertyType) ?? _property.PropertyType;
                _converter = _property.Converter;
                _converterContext = new PropertyTypeDescriptorContext(_target, _property);
                _isEnabled = enabled;

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

            public string Name => _property.DisplayName;

            public string Category => string.IsNullOrWhiteSpace(_property.Category) ? "General" : _property.Category;

            public string Description => _property.Description;

            public bool IsReadOnly => _property.IsReadOnly;

            public bool IsEditable => _isEnabled && !IsReadOnly;

            public bool IsBoolEditorVisible => _valueType == typeof(bool);

            public bool IsEnumEditorVisible => _valueType.IsEnum;

            private bool IsColorType =>
                _valueType == typeof(System.Drawing.Color) ||
                _valueType.FullName == "System.Windows.Media.Color";

            public bool IsTextEditorVisible => !IsBoolEditorVisible && !IsEnumEditorVisible && !IsStandardValuesEditorVisible;

            public IList EnumValues { get; }

            public IList StandardValues { get; }

            public bool IsStandardValuesEditorVisible => StandardValues != null && (_isStandardValuesExclusive || IsColorType);

            public bool IsColorPreviewVisible => IsColorType;

            public Brush ColorPreviewBrush
            {
                get
                {
                    var value = _property.GetValue(_target);
                    if (value is System.Drawing.Color drawingColor)
                        return new SolidColorBrush(Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B));

                    if (value is Color mediaColor)
                        return new SolidColorBrush(mediaColor);

                    return Brushes.Transparent;
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

            public string ValueText
            {
                get
                {
                    var value = _property.GetValue(_target);
                    if (value == null)
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
                    if (!IsEditable)
                        return;

                    try
                    {
                        object parsed;

                        if (_valueType == typeof(string))
                        {
                            parsed = value;
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