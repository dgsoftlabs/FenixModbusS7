using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for AddTag.xaml
    /// </summary>
    public partial class AddTag : Window, INotifyPropertyChanged
    {
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                propChanged += value;
            }

            remove
            {
                propChanged -= value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the range is active.
        /// </summary>
        public bool RangeAct { get; set; }

        /// <summary>
        /// Gets or sets the range.
        /// </summary>
        public int Range { get; set; }

        private string TagName_;

        /// <summary>
        /// Gets or sets the tag name.
        /// </summary>
        public string TagName
        {
            get { return TagName_; }
            set
            {
                //Pszeszukanie tagow
                if (project.tagsList.Exists(x => x.tagName == value) || project.InTagsList.Exists(x => x.tagName == value))
                    throw new ArgumentException("Change name of tag becuse is already exists!");

                TagName_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TagName)));
            }
        }

        private BytesOrder BtOrder_;

        /// <summary>
        /// Gets or sets the bytes order.
        /// </summary>
        public BytesOrder BtOrder
        {
            get { return BtOrder_; }
            set
            {
                BtOrder_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BtOrder)));
            }
        }

        private TypeData TpData_;

        /// <summary>
        /// Gets or sets the type of data.
        /// </summary>
        public TypeData TpData
        {
            get { return TpData_; }
            set
            {
                TpData_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TpData)));
                RefreshSecondAdress();
            }
        }

        private MemoryAreaInfo SelArea_;

        /// <summary>
        /// Gets or sets the selected memory area.
        /// </summary>
        public MemoryAreaInfo SelArea
        {
            get { return SelArea_; }
            set
            {
                SelArea_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelArea)));
                RefreshSecondAdress();
            }
        }

        private bool DbBlockAct_;

        /// <summary>
        /// Gets or sets a value indicating whether the database block is active.
        /// </summary>
        public bool DbBlockAct
        {
            get { return DbBlockAct_; }
            set
            {
                DbBlockAct_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DbBlockAct)));
            }
        }

        private ObservableCollection<MemoryAreaInfo> MemArList_;

        /// <summary>
        /// Gets or sets the memory area list.
        /// </summary>
        public ObservableCollection<MemoryAreaInfo> MemArList
        {
            get { return MemArList_; }
            set { MemArList_ = value; }
        }

        private int DbAdress_;

        /// <summary>
        /// Gets or sets the database address.
        /// </summary>
        public int DbAdress
        {
            get { return DbAdress_; }
            set
            {
                DbAdress_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DbAdress)));
            }
        }

        private int Adress_;

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        public int Adress
        {
            get { return Adress_; }
            set
            {
                Adress_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Adress)));
            }
        }

        private int SecAdress_;

        /// <summary>
        /// Gets or sets the secondary address.
        /// </summary>
        public int SecAdress
        {
            get { return SecAdress_; }
            set
            {
                SecAdress_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SecAdress)));
            }
        }

        private ObservableCollection<int> ScAdrList_ = new ObservableCollection<int>();

        /// <summary>
        /// Gets or sets the secondary address list.
        /// </summary>
        public ObservableCollection<int> ScAdreList
        {
            get { return ScAdrList_; }
            set { ScAdrList_ = value; }
        }

        private string Desc_;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Desc
        {
            get { return Desc_; }
            set
            {
                Desc_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Desc)));
            }
        }

        private ProjectContainer projectContainer;
        private Project project;
        private Guid projectId = Guid.Empty;
        private Guid deviceId = Guid.Empty;
        private GlobalConfiguration globalConfig = new GlobalConfiguration();
        private IDriverModel driverModel;
        private Connection connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddTag"/> class.
        /// </summary>
        /// <param name="prCon">The project container.</param>
        /// <param name="prId">The project ID.</param>
        /// <param name="devId">The device ID.</param>
        public AddTag(ref ProjectContainer prCon, Guid prId, Guid devId)
        {
            try
            {
                DataContext = this;
                Range = 2;
                projectContainer = prCon;
                projectId = prId;
                project = prCon.getProject(prId);
                deviceId = devId;
                Device Dev = prCon.getDevice(prId, devId);
                connection = prCon.getConnection(prId, Dev.parentId);
                driverModel = connection.Idrv;

                TpData = TypeData.SHORT;
                MemArList = new ObservableCollection<MemoryAreaInfo>(driverModel.MemoryAreaInf);
                SelArea = driverModel.MemoryAreaInf.First();

                InitializeComponent();

                if (driverModel.driverName == "S7-300-400 Ethernet")
                {
                    DbBlockAct = true;
                    BtOrder = BytesOrder.DCBA;
                }
                else
                {
                    DbBlockAct = false;
                    BtOrder = BytesOrder.BADC;
                }

                string nm = "Tag";
                for (int x = 0; ; x++)
                {
                    if (projectContainer.GetAllITags().Exists(k => k.Name == nm))
                        nm = $"{nm}{x}";
                    else
                        break;
                }

                TagName = nm;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Refreshes the secondary address list based on the selected memory area and type of data.
        /// </summary>
        private void RefreshSecondAdress()
        {
            try
            {
                int buff = SecAdress;
                ScAdreList.Clear();
                if (TpData == TypeData.BIT)
                {
                    if (SelArea?.AdresSize > 1)
                    {
                        //Dodanie elementu do wyboru
                        for (int i = 0; i < SelArea.AdresSize; i++)
                        {
                            ScAdreList.Add(i);
                        }
                    }
                }
                else if (TpData == TypeData.BYTE || TpData == TypeData.SBYTE)
                {
                    if (SelArea?.AdresSize > 8)
                    {
                        for (int i = 0; i < SelArea.AdresSize / 8; i++)
                        {
                            ScAdreList.Add(i);
                        }
                    }
                }
                else if (TpData == TypeData.CHAR || TpData == TypeData.SHORT || TpData == TypeData.USHORT)
                {
                    if (SelArea?.AdresSize > 16)
                    {
                        for (int i = 0; i < SelArea.AdresSize / 16; i++)
                        {
                            ScAdreList.Add(i);
                        }
                    }
                }
                else if (TpData == TypeData.INT || TpData == TypeData.UINT || TpData == TypeData.FLOAT)
                {
                    if (SelArea?.AdresSize > 32)
                    {
                        for (int i = 0; i < SelArea.AdresSize / 32; i++)
                        {
                            ScAdreList.Add(i);
                        }
                    }
                }
                else if (TpData == TypeData.DOUBLE)
                {
                    if (SelArea?.AdresSize > 64)
                    {
                        for (int i = 0; i < SelArea.AdresSize / 64; i++)
                        {
                            ScAdreList.Add(i);
                        }
                    }
                }

                if (ScAdreList.Count > 0)
                {
                    if (ScAdreList.ToList().Exists(x => x == buff))
                    {
                        SecAdress = buff;
                    }
                    else
                    {
                        SecAdress = ScAdreList.First();
                    }
                }
                else
                {
                    SecAdress = 0;
                }
            }
            catch (Exception Ex)
            {
                if (projectContainer.ApplicationError != null)
                {
                    projectContainer.ApplicationError(this, new ProjectEventArgs(Ex));
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Button_Save control.
        /// Adds a new tag to the project based on the input values.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            if (!RangeAct)
            {
                Tag tg1 = new Tag(TagName, BtOrder, SelArea.Name, Adress, SecAdress, Desc, TpData, driverModel, connection.objId, DbAdress);
                tg1.BlockAdress = DbAdress;
                projectContainer.addTag(projectId, deviceId, tg1);
            }
            else
            {
                Tag adressTag = null;

                var colors = ColorsManager.trendColors;
                for (int i = 0; i < Range; i++)
                {
                    string nm = $"{TagName}{i}";
                    for (int x = 0; ; x++)
                    {
                        if (projectContainer.GetAllITags().Exists(k => k.Name == nm))
                            nm = $"{TagName}{x}";
                        else
                            break;
                    }

                    if (adressTag == null)
                    {
                        Tag tg = new Tag(nm, BtOrder, SelArea.Name, Adress, SecAdress, Desc, TpData, driverModel, connection.objId, DbAdress);
                        tg.BlockAdress = DbAdress;

                        if (i < colors.Length)
                        {
                            tg.Clr = System.Drawing.Color.FromArgb(colors[i].Red, colors[i].Green, colors[i].Blue);
                        }

                        projectContainer.addTag(projectId, deviceId, tg);
                        adressTag = tg;
                    }
                    else
                    {
                        Tag tg = new Tag(nm, BtOrder, SelArea.Name, adressTag.nextAdress().adress, adressTag.nextAdress().secAdress, Desc, TpData, driverModel, connection.objId, DbAdress);
                        tg.BlockAdress = DbAdress;

                        if (i < colors.Length)
                        {
                            tg.Clr = System.Drawing.Color.FromArgb(colors[i].Red, colors[i].Green, colors[i].Blue);
                        }

                        projectContainer.addTag(projectId, deviceId, tg);
                        adressTag = tg;
                    }
                }
            }

            Close();
        }

        /// <summary>
        /// Handles the Click event of the Button_Close control.
        /// Closes the AddTag window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}