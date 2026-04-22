using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for Output.xaml
    /// </summary>
    public partial class Output : UserControl, INotifyPropertyChanged
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

        //Konstruktor
        public Output(ProjectContainer prCon, object listaAlarmow)
        {
            InitializeComponent();

            DataContext = this;

            //Kontener projektowy
            PrCon = prCon;

            ((ObservableCollection<CustomException>)listaAlarmow).CollectionChanged += Output_CollectionChanged;
        }

        private void Output_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //if(mScroll)
            //{
            //    if (View.Items.Count > 0)
            //    {
            //        var border = VisualTreeHelper.GetChild(View, 0) as Decorator;
            //        if (border != null)
            //        {
            //            var scroll = border.Child as ScrollViewer;
            //            if (scroll != null) scroll.ScrollToEnd();
            //        }
            //    }
            //}
        }

        //Wyczysc
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

        public override string ToString()
        {
            return "Output";
        }

        //dodano nowy element
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