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
            AdjustColumns();
        }

        private void PropertyList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustColumns();
        }

        private void AdjustColumns()
        {
            if (PropertyList?.View is not GridView gridView || gridView.Columns.Count < 2)
                return;

            var totalWidth = PropertyList.ActualWidth - 6;
            if (totalWidth <= 0)
                return;

            var nameColumn = gridView.Columns[0];
            var valueColumn = gridView.Columns[1];
            var nameWidth = nameColumn.ActualWidth > 0 ? nameColumn.ActualWidth : nameColumn.Width;
            valueColumn.Width = Math.Max(120, totalWidth - nameWidth);
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
            private readonly DelegateCommand _editCollectionCommand;
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
                _editCollectionCommand = new DelegateCommand(_ => EditCollection(null), _ => IsEditable && IsCollectionEditorVisible);

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

            public bool IsTextEditorVisible => !IsBoolEditorVisible && !IsEnumEditorVisible && !IsStandardValuesEditorVisible && !IsCollectionEditorVisible;

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

            public ICommand EditCollectionCommand => _editCollectionCommand;

            public string ValueText
            {
                get
                {
                    if (IsCollectionEditorVisible)
                        return CollectionSummary;

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
                if (!IsEditable || !IsCollectionEditorVisible)
                    return;

                if (_property.GetValue(_target) is not List<CustomTimer> timers)
                    return;

                var editor = new CustomTimerEditorWindow(timers)
                {
                    Owner = owner
                };

                if (editor.ShowDialog() != true)
                    return;

                _property.SetValue(_target, editor.GetResult());
                OnPropertyChanged(nameof(CollectionSummary));
                OnPropertyChanged(nameof(ValueText));
            }

            public void UpdateEnabledState(bool enabled)
            {
                _isEnabled = enabled;
                OnPropertyChanged(nameof(IsEditable));
                _editCollectionCommand.RaiseCanExecuteChanged();
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

        private sealed class CustomTimerEditorWindow : Window
        {
            private readonly ObservableCollection<CustomTimerRow> _rows;
            private readonly DataGrid _grid;

            public CustomTimerEditorWindow(IEnumerable<CustomTimer> source)
            {
                Title = "Timers";
                Width = 560;
                Height = 360;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.CanResize;

                _rows = new ObservableCollection<CustomTimerRow>(source.Select(t => new CustomTimerRow
                {
                    Name = t.Name,
                    Time = t.Time,
                    Delay = t.Delay
                }));

                var layout = new Grid { Margin = new Thickness(10) };
                layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
                var addButton = new Button { Content = "Add", MinWidth = 70, Margin = new Thickness(0, 0, 0, 0) };
                var removeButton = new Button { Content = "Remove", MinWidth = 70, Margin = new Thickness(8, 0, 0, 0) };
                addButton.Click += (_, _) => _rows.Add(new CustomTimerRow { Name = "Timer", Time = 1000, Delay = 0 });
                removeButton.Click += (_, _) =>
                {
                    if (_grid.SelectedItem is CustomTimerRow row)
                        _rows.Remove(row);
                };
                toolbar.Children.Add(addButton);
                toolbar.Children.Add(removeButton);

                _grid = new DataGrid
                {
                    ItemsSource = _rows,
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                _grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new System.Windows.Data.Binding(nameof(CustomTimerRow.Name)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                _grid.Columns.Add(new DataGridTextColumn { Header = "Time [ms]", Binding = new System.Windows.Data.Binding(nameof(CustomTimerRow.Time)), Width = new DataGridLength(130) });
                _grid.Columns.Add(new DataGridTextColumn { Header = "Delay [ms]", Binding = new System.Windows.Data.Binding(nameof(CustomTimerRow.Delay)), Width = new DataGridLength(130) });

                var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var okButton = new Button { Content = "OK", MinWidth = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
                var cancelButton = new Button { Content = "Cancel", MinWidth = 80, IsCancel = true };
                okButton.Click += (_, _) =>
                {
                    DialogResult = true;
                    Close();
                };
                cancelButton.Click += (_, _) =>
                {
                    DialogResult = false;
                    Close();
                };
                footer.Children.Add(okButton);
                footer.Children.Add(cancelButton);

                Grid.SetRow(toolbar, 0);
                Grid.SetRow(_grid, 1);
                Grid.SetRow(footer, 2);
                layout.Children.Add(toolbar);
                layout.Children.Add(_grid);
                layout.Children.Add(footer);
                Content = layout;
            }

            public List<CustomTimer> GetResult()
            {
                return _rows.Select(r => new CustomTimer
                {
                    Name = r.Name ?? string.Empty,
                    Time = r.Time,
                    Delay = r.Delay
                }).ToList();
            }
        }

        private sealed class CustomTimerRow
        {
            public string Name { get; set; }

            public int Time { get; set; }

            public int Delay { get; set; }
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public DelegateCommand(Action<object> execute, Predicate<object> canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return _canExecute?.Invoke(parameter) ?? true;
            }

            public void Execute(object parameter)
            {
                _execute?.Invoke(parameter);
            }

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}