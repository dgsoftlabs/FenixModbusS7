using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProjectDataLib.Data;

namespace ProjectDataLib
{
    [Serializable]
    public class DatabaseModel : ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
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

        [Browsable(false), XmlIgnore]
        public ObservableCollection<object> Children
        {
            get
            {
                return new ObservableCollection<object>();
            }

            set
            {
            }
        }

        [JsonIgnore]
        private ProjectContainer PrCon_ { get; set; }

        [Browsable(false), XmlIgnore, JsonIgnore]
        public ProjectContainer PrCon
        {
            get { return PrCon_; }
            set { PrCon_ = value; }
        }

        [field: NonSerialized]
        private Project Pr_;

        [Browsable(false), JsonIgnore, XmlIgnore]
        public Project Pr
        {
            get { return Pr_; }
            set
            {
                Pr_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Proj"));
            }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }

        [Browsable(false), XmlIgnore()]
        private ITagRepository _repository;

        public DatabaseModel()
        {
        }

        public DatabaseModel(ProjectContainer prCon, Project pr)
        {
            PrCon = prCon;
            Pr = pr;

            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var dbPath = Path.GetDirectoryName(Pr.path) + PrCon.Database;
                
                _repository = new TagRepository();
                await _repository.InitializeAsync(dbPath);

                Pr.Db.IsLive = false;

                ((ITableView)Pr).Children.CollectionChanged += ITags_CollectionChanged;

                foreach (ITag tg in ((ITableView)Pr).Children)
                    ((INotifyPropertyChanged)tg).PropertyChanged += ITag_PropertyChanged;
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private void ITags_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                ((INotifyPropertyChanged)e.NewItems[0]).PropertyChanged += ITag_PropertyChanged;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                ((INotifyPropertyChanged)e.OldItems[0]).PropertyChanged -= ITag_PropertyChanged;
                RemoveDataITagElementAsync(((ITag)e.OldItems[0]).Name).GetAwaiter().GetResult();
            }
        }

        private void ITag_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                ITag tg = (ITag)sender;

                if (tg.TypeData_ == TypeData.CHAR)
                {
                    AddDataElementAsync(tg.Name, Char.GetNumericValue((char)tg.Value).ToString(), DateTime.Now).GetAwaiter().GetResult();
                }
                else if (tg.TypeData_ == TypeData.BIT)
                {
                    AddDataElementAsync(tg.Name, ((bool)tg.Value) ? "1.0" : "0.0", DateTime.Now).GetAwaiter().GetResult();
                }
                else
                {
                    AddDataElementAsync(tg.Name, tg.Value.ToString(), DateTime.Now).GetAwaiter().GetResult();
                }
            }
        }

        public void SaveSnapshot()
        {
            SaveSnapshotAsync().GetAwaiter().GetResult();
        }

        public async Task SaveSnapshotAsync()
        {
            try
            {
                if (_repository == null)
                    return;

                DateTime stamp = DateTime.Now;

                var batch = new List<(string Name, double Value)>();

                foreach (ITag tg in ((ITableView)Pr).Children)
                {
                    string raw;
                    if (tg.TypeData_ == TypeData.CHAR)
                        raw = Char.GetNumericValue((char)tg.Value).ToString();
                    else if (tg.TypeData_ == TypeData.BIT)
                        raw = ((bool)tg.Value) ? "1.0" : "0.0";
                    else
                        raw = tg.Value.ToString();

                    if (double.TryParse(raw.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double numValue))
                        batch.Add((tg.Name, numValue));
                }

                if (batch.Count > 0)
                    await _repository.AddTagsBatchAsync(batch, stamp);
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private async Task AddDataElementAsync(string name, string value, DateTime tm)
        {
            try
            {
                if (_repository != null && double.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var numValue))
                {
                    await _repository.AddTagAsync(name, numValue, tm);
                }
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private async Task RemoveDataITagElementAsync(string name)
        {
            try
            {
                if (_repository != null)
                {
                    await _repository.RemoveTagByNameAsync(name);
                }
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        public void Reset()
        {
            if (_repository != null)
            {
                if (MessageBox.Show("Do you want to remove all records from database?",
                                    "Database",
                                      MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    ResetDatabaseAsync().GetAwaiter().GetResult();
                    MessageBox.Show("All records removed!");
                }
            }
        }

        private async Task ResetDatabaseAsync()
        {
            await _repository.ClearAllTagsAsync();
        }

        public List<TagDTO> GetAll()
        {
            return GetAllAsync().GetAwaiter().GetResult();
        }

        public async Task<List<TagDTO>> GetAllAsync()
        {
            if (_repository == null)
                return new List<TagDTO>();
            
            return await _repository.GetAllTagsAsync();
        }

        public ObservableCollection<TagDTO> GetAllObservableCollection()
        {
            var tags = GetAllAsync().GetAwaiter().GetResult();
            return new ObservableCollection<TagDTO>(tags);
        }

        public DatabaseValues GetRange(ITag tg, DateTime from, DateTime to)
        {
            return GetRangeAsync(tg, from, to).GetAwaiter().GetResult();
        }

        public async Task<DatabaseValues> GetRangeAsync(ITag tg, DateTime from, DateTime to)
        {
            var tags = await _repository.GetTagsByNameAsync(tg.Name, from, to, descending: false);
            var dbVls = new DatabaseValues();

            foreach (var tagDto in tags)
            {
                double dtCurr = tagDto.Stamp.ToOADate();
                double value = tagDto.Value;

                bool lastPoint = false;
                if (tg.TypeData_ == TypeData.BIT)
                {
                    if (dbVls.ValPts.Count != 0)
                        lastPoint = Convert.ToBoolean(dbVls.ValPts.Last());

                    if (lastPoint != Convert.ToBoolean(value))
                    {
                        dbVls.TimePts.Add(dtCurr);
                        dbVls.ValPts.Add(Convert.ToDouble(lastPoint));

                        dbVls.TimePts.Add(dtCurr);
                        dbVls.ValPts.Add(value);
                    }
                    else
                    {
                        dbVls.TimePts.Add(dtCurr);
                        dbVls.ValPts.Add(value);
                    }
                }
                else
                {
                    dbVls.TimePts.Add(dtCurr);
                    dbVls.ValPts.Add(value);
                }
            }

            return dbVls;
        }

        private bool IsLive_;

        [XmlIgnore]
        [Browsable(false)]
        public bool IsLive
        {
            get { return IsLive_; }
            set
            {
                IsLive_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLive)));
            }
        }

        [ReadOnly(true)]
        public string Name
        {
            get
            {
                return "Database";
            }

            set
            {
            }
        }

        [Browsable(false)]
        public bool IsExpand
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        [Browsable(false)]
        public bool IsBlocked
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public void OnDeserializedXML()
        {
            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        public ObservableCollection<TagDTO> GetDataByStamp(DateTime from, DateTime to, bool descending = true)
        {
            var tags = GetDataByStampAsync(from, to, descending).GetAwaiter().GetResult();
            return new ObservableCollection<TagDTO>(tags);
        }

        public async Task<List<TagDTO>> GetDataByStampAsync(DateTime from, DateTime to, bool descending = true)
        {
            if (_repository == null)
                return new List<TagDTO>();

            return await _repository.GetTagsByStampAsync(from, to, descending);
        }

        public async Task<List<TagDTO>> GetAllTagsAsync(bool descending = true)
        {
            if (_repository == null)
                return new List<TagDTO>();

            return await _repository.GetAllTagsAsync(descending);
        }
    }
}
