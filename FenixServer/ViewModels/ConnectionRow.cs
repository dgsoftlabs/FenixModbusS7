using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FenixServer.ViewModels
{
    public sealed class ConnectionRow : INotifyPropertyChanged
    {
        private string _status;

        public Guid SourceId { get; set; }
        public string Kind { get; set; }
        public string Name { get; set; }
        public string Protocol { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged();
            }
        }

        public int Sent { get; set; }
        public int Received { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}