using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fenix
{
    public partial class OutputView : UserControl, INotifyPropertyChanged
    {
        private ProjectContainer PrCon;

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

        private bool mScroll_ = true;

        public Boolean mScroll
        { get { return mScroll_; } set { mScroll_ = value; propChanged_?.Invoke(this, new PropertyChangedEventArgs("mScroll")); } }

        public OutputView(ProjectContainer prCon, object listaAlarmow)
        {
            InitializeComponent();

            DataContext = this;

            PrCon = prCon;
        }

        private void Button_Clr_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ObservableCollection<CustomException> list = (ObservableCollection<CustomException>)View.DataContext;
                list.Clear();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Button_Copy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (View.SelectedItem is CustomException item)
                {
                    string text = $"{item.Sender}\t{item.Czas}\t{item.Ex?.Message}\t{item.Ex}";
                    Clipboard.SetText(text);
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        public override string ToString()
        {
            return "Output";
        }

        private void View_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            try
            {
                if (View.Items.Count > 0 && mScroll)
                {
                    var border = VisualTreeHelper.GetChild(View, 0) as Decorator;
                    if (border != null)
                    {
                        var scroll = border.Child as ScrollViewer;
                        if (scroll != null) scroll.ScrollToEnd();
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }
    }
}