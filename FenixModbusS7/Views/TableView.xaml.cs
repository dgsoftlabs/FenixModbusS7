using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using AvalonDock.Layout;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for DefTableView.xaml
    /// </summary>
    public partial class TableView : UserControl, INotifyPropertyChanged
    {
        private int index;

        private ProjectContainer PrCon_;

        public ProjectContainer PrCon
        {
            get { return PrCon_; }
            set
            {
                PrCon_ = value;
                propChanged_?.Invoke(this, new PropertyChangedEventArgs("PrCon"));
            }
        }

        private Project Pr_;

        public Project Pr
        {
            get { return Pr_; }
            set
            {
                Pr_ = value;
                propChanged_?.Invoke(this, new PropertyChangedEventArgs("Pr"));
            }
        }

        private Connection Con_;

        public Connection Con
        {
            get { return Con_; }
            set { Con_ = value; }
        }

        private Device Dev_;

        public Device Dev
        {
            get { return Dev_; }
            set { Dev_ = value; }
        }

        private PropertyChangedEventHandler propChanged_;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                propChanged_ += value;
            }

            remove
            {
                propChanged_ -= value;
            }
        }

        private ObservableCollection<ITag> ITagList;
        private LayoutAnchorable Win;
        private ElementKind elKind;
        private Guid Sel;

        //Konstruktor
        public TableView(ProjectContainer prCon, Guid pr, Guid sel, ElementKind elkind, LayoutAnchorable win)
        {
            try
            {
                InitializeComponent();

                PrCon = prCon;
                Pr = PrCon.projectList.First();
                Sel = sel;
                elKind = elkind;
                Win = win;

                View.DataContext = this;
                //Potrzebne dla contextu dla column
                ((FrameworkElement)Resources["ProxyElement"]).DataContext = this;

                //dataselection
                if (elKind == ElementKind.Project)
                {
                    //Title
                    Win.Title = Pr.projectName;

                    //Dane
                    ITagList = ((ITableView)Pr).Children;
                }
                else if (elKind == ElementKind.Connection)
                {
                    if (Pr.connectionList.Exists(x => x.objId == Sel))
                    {
                        //Title
                        Con = PrCon.getConnection(Pr.objId, Sel);
                        Win.Title = Pr.projectName + "." + Con.connectionName;

                        //Dane
                        ITagList = ((ITableView)Con).Children;

                        //Zdarzenia
                        ((INotifyPropertyChanged)Con).PropertyChanged += Pr_Conn_Dev_PropChanged;
                        ((ITreeViewModel)Con).Children.CollectionChanged += Dev_CollectionChanged;
                    }
                    else
                    {
                        Win.Close();
                    }
                }
                else if (elKind == ElementKind.Device)
                {
                    if (Pr.DevicesList.Exists(x => x.objId == Sel))
                    {
                        //Title
                        Dev = PrCon.getDevice(Pr.objId, Sel);
                        Con = PrCon.getConnection(Pr.objId, Dev.parentId);
                        Win.Title = Pr.projectName + "." + Con.connectionName + "." + Dev.name;

                        //Dane
                        ITagList = ((ITableView)Dev).Children;

                        ((INotifyPropertyChanged)Con).PropertyChanged += Pr_Conn_Dev_PropChanged;
                        ((INotifyPropertyChanged)Dev).PropertyChanged += Pr_Conn_Dev_PropChanged;
                    }
                    else
                    {
                        Win.Close();
                    }
                }

                //Zdarzenia
                Win.Closing += Win_Closing;
                ((INotifyPropertyChanged)Pr).PropertyChanged += Pr_Conn_Dev_PropChanged;
                ((ITreeViewModel)Pr).Children.CollectionChanged += ProjectChildrenChanged;

                index = PrCon.winManagment.Count;
                PrCon.winManagment.Add(new WindowsStatus(index, true, false));

                View.ItemsSource = ITagList;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Zdarzenie nasłuchujące na projekcie
        private void ProjectChildrenChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    if (e.OldItems[0] is Connection)
                    {
                        Connection cn = (Connection)e.OldItems[0];
                        if (elKind == ElementKind.Connection)
                        {
                            if (cn.objId == Sel)
                            {
                                if (Con != null)
                                    ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                                Win.Close();
                            }
                        }
                        else if (elKind == ElementKind.Device)
                        {
                            if (cn.objId == Dev.parentId)
                            {
                                if (Con != null)
                                    ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;

                                if (Dev != null)
                                    ((INotifyPropertyChanged)Dev).PropertyChanged -= Pr_Conn_Dev_PropChanged;

                                Win.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Device changed
        private void Dev_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    if (e.OldItems[0] is Device)
                    {
                        Device dev = (Device)e.OldItems[0];
                        if (elKind == ElementKind.Connection)
                        {
                            if (Dev != null)
                            {
                                if (Dev.objId == dev.objId)
                                    ((INotifyPropertyChanged)Dev).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Zmiana wlasciwosci projektu badz polocznia
        private void Pr_Conn_Dev_PropChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                //Zabezpiecznie
                if (e.PropertyName != "Name" && e.PropertyName != "name")
                    return;

                //dataselection
                if (elKind == ElementKind.Project)
                {
                    Win.Title = Pr.projectName;
                }
                else if (elKind == ElementKind.Connection)
                {
                    //Typy
                    Connection cn = PrCon.getConnection(Pr.objId, Sel);

                    //Title
                    if (cn != null && Pr != null)
                        Win.Title = Pr.projectName + "." + cn.connectionName;
                }
                else if (elKind == ElementKind.Device)
                {
                    //Typy bazowe
                    Device dev = PrCon.getDevice(Pr.objId, Sel);
                    Connection cn = PrCon.getConnection(Pr.objId, dev.parentId);

                    //Title
                    if (cn != null && Pr != null && dev != null)
                        Win.Title = Pr.projectName + "." + cn.connectionName + "." + dev.name;
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Zamykanie okna
        private void Win_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (elKind == ElementKind.Connection)
                {
                    if (Con != null)
                    {
                        ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                        ((ITreeViewModel)Con).Children.CollectionChanged -= Dev_CollectionChanged;
                    }
                }
                else if (elKind == ElementKind.Device)
                {
                    //Zdarzenie
                    if (Con != null)
                        ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;

                    if (Dev != null)
                        ((INotifyPropertyChanged)Dev).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                }

                ((INotifyPropertyChanged)Pr).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                ((ITreeViewModel)Pr).Children.CollectionChanged -= ProjectChildrenChanged;
                Win.Closing -= Win_Closing;

                PrCon.winManagment.RemoveAll(x => x.index == index);
            }
            catch (Exception Ex)
            {
                //Zdarzenia
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //BitBytes
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            Tag tg = (Tag)((ComboBox)sender).DataContext;

            MemoryAreaInfo info = (from x in tg.idrv.MemoryAreaInf where x.Name == tg.areaData select x).First();

            switch (tg.TypeData)
            {
                case TypeData.BIT:

                    if (info.AdresSize > 1)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }

                    break;

                case TypeData.BYTE:
                    if (info.AdresSize > 8)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 8; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;

                case TypeData.SBYTE:
                    if (info.AdresSize > 8)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 8; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    };
                    break;

                case TypeData.CHAR:
                    if (info.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 16; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;

                case TypeData.SHORT:
                    if (info.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 16; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;

                case TypeData.USHORT:
                    if (info.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 16; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;

                case TypeData.INT:
                    if (info.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 32; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;

                case TypeData.UINT:
                    if (info.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 32; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;

                case TypeData.FLOAT:
                    if (info.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 32; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;

                case TypeData.DOUBLE:

                    if (info.AdresSize > 64)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < info.AdresSize / 64; i++)
                            buff.Add(i);

                        ((ComboBox)sender).ItemsSource = buff;
                    }
                    else
                    {
                        ((ComboBox)sender).ItemsSource = new List<int>() { 0 };
                    }
                    break;
            }
        }

        //DataTypes
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).BindingGroup != null)
                ((ComboBox)sender).BindingGroup.CommitEdit();
        }

        //SetValue Binary Switch
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is Tag)
            {
                Tag tg = (Tag)((Button)sender).DataContext;

                if (tg.idrv.isAlive)
                {
                    if ((Boolean)tg.value)
                        tg.setValueMethod(false);
                    else
                        tg.setValueMethod(true);
                }
            }
            else
            {
                //InTag
            }
        }

        //Other fields
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).BindingGroup != null)
                ((TextBox)sender).BindingGroup.CommitEdit();
        }

        //Color
        private void ColorPicker_SelectedColorChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb)
                    cb.BindingGroup?.CommitEdit();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Zmiana nazwy
        private void NameTemp_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tg = ((TextBox)sender).DataContext;

            if (tg is Tag)
            {
                Project pr = ((Tag)tg).Proj;
                TextBox tb = ((TextBox)sender);

                if (pr.tagsList.Exists(x => x.tagName == tb.Text) || pr.InTagsList.Exists(x => x.tagName == tb.Text))
                {
                    tb?.BindingGroup?.CancelEdit();
                }
                else
                {
                    tb?.BindingGroup?.CommitEdit();
                }
            }
            else if (tg is InTag)
            {
                Project pr = ((InTag)tg).Proj;
                TextBox tb = ((TextBox)sender);

                if (pr.tagsList.Exists(x => x.tagName == tb.Text) || pr.InTagsList.Exists(x => x.tagName == tb.Text))
                {
                    tb?.BindingGroup?.CancelEdit();
                }
                else
                {
                    tb?.BindingGroup?.CommitEdit();
                }
            }
        }

        //Grubosc linii na wykresie
        private void IntegerUpDown_ValueChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is TextBox tb)
                    tb.BindingGroup?.CommitEdit();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Short Data Chanched
        private void shtxt_ValueChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;

                if (tb.DataContext is Tag tg)
                {
                    if (tg.idrv.isAlive && short.TryParse(tb.Text, out var value))
                    {
                        tg.setValueMethod(value);
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Byte Data Chanched
        private void bttxt_ValueChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;

                if (tb.DataContext is Tag tg)
                {
                    if (tg.idrv.isAlive && byte.TryParse(tb.Text, out var value))
                    {
                        tg.setValueMethod(value);
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Single Data Chanched
        private void sntxt_ValueChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;

                if (tb.DataContext is Tag tg)
                {
                    if (tg.idrv.isAlive && float.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        tg.setValueMethod(value);
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Double
        private void doubtxt_ValueChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;

                if (tb.DataContext is Tag tg)
                {
                    if (tg.idrv.isAlive && double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        tg.setValueMethod(value);
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //int
        private void intxt_ValueChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;

                if (tb.DataContext is Tag tg)
                {
                    if (tg.idrv.isAlive && int.TryParse(tb.Text, out var value))
                    {
                        tg.setValueMethod(value);
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Text Changed
        private void txt_ValueChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;

                if (tb.DataContext is Tag tg)
                {
                    if (tg.idrv.isAlive)
                    {
                        try
                        {
                            tg.setValueMethod(tb.Text);
                        }
                        catch (Exception Ex)
                        {
                            MessageBox.Show(Ex.Message);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }
    }

    public class BitByteConv : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ITag itg = ((ITag)value);

            if (!itg.ActBitByte)
                return new List<int> { 0 };

            Tag tg = ((Tag)itg);

            MemoryAreaInfo mArea = (from x in tg.idrv.MemoryAreaInf where x.Name == tg.areaData select x).First();

            switch (tg.TypeData)
            {
                case TypeData.BIT:

                    if (mArea.AdresSize > 1)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.BYTE:
                    if (mArea.AdresSize > 8)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 8; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.SBYTE:
                    if (mArea.AdresSize > 8)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 8; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    };

                case TypeData.CHAR:
                    if (mArea.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 16; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.SHORT:
                    if (mArea.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 16; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.USHORT:
                    if (mArea.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 16; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.INT:
                    if (mArea.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 32; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.UINT:
                    if (mArea.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 32; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.FLOAT:
                    if (mArea.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 32; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.DOUBLE:
                    if (mArea.AdresSize > 64)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 64; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }
            }

            return new int[] { 0 };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AreaDataConv : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ITag tgx = (ITag)value;

            if (!tgx.ActAreaData)
                return new List<string>() { "" };

            return (from x in ((Tag)tgx).idrv.MemoryAreaInf select x.Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ValueConv : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (((ITag)values[1]).GetFormatedValue());
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { value };
        }
    }

    public class ColorConv : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color cl)
                return new SolidColorBrush(Color.FromArgb(cl.A, cl.R, cl.G, cl.B));

            if (value is Color mediaColor)
                return new SolidColorBrush(mediaColor);

            return Brushes.Transparent;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
                return System.Drawing.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);

            if (value is Color cl)
                return System.Drawing.Color.FromArgb(cl.A, cl.R, cl.G, cl.B);

            return System.Drawing.Color.Transparent;
        }
    }

    public class ColorBrushConv : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color cl)
                return new SolidColorBrush(Color.FromArgb(cl.A, cl.R, cl.G, cl.B));

            if (value is Color mediaColor)
                return new SolidColorBrush(mediaColor);

            return Brushes.Transparent;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
                return System.Drawing.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);

            if (value is Color cl)
                return System.Drawing.Color.FromArgb(cl.A, cl.R, cl.G, cl.B);

            return System.Drawing.Color.Transparent;
        }
    }

    public class ValueMarkConv : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value)
                    return new SolidColorBrush(Colors.Green);
                else
                    return new SolidColorBrush(Colors.Red);
            }
            else
                return new SolidColorBrush(Colors.White);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RowDataConv : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ITag)value).GrVisibleTab;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}