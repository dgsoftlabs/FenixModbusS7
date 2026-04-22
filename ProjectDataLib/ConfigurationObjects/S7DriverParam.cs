using System;
using System.ComponentModel;
using System.Net;
using System.Windows.Forms;

namespace ProjectDataLib
{
    public enum S7CpuSeries
    {
        S7300,
        S7400
    }

    [Serializable]
    public class S7DriverParam
    {
        private IPAddress Ip_;

        [Category("Target")]
        [DisplayName("Address Ip")]
        public String Ip
        {
            get { return Ip_.ToString(); }
            set
            {
                try { Ip_ = IPAddress.Parse(value); }
                catch (Exception Ex) { MessageBox.Show(Ex.Message); }
            }
        }

        private ushort Port_;

        [Category("Target")]
        [DisplayName("Port")]
        public ushort Port
        {
            set { Port_ = value; }
            get { return Port_; }
        }

        private int Rack_;

        [Category("Target")]
        [DisplayName("Rack")]
        public int Rack
        {
            get { return Rack_; }
            set { Rack_ = value; }
        }

        private int Slot_;

        [Category("Target")]
        [DisplayName("Slot")]
        public int Slot
        {
            get { return Slot_; }
            set { Slot_ = value; }
        }

        private int Timeout_;

        [Category("Time")]
        [DisplayName("Timeout [ms]")]
        public int Timeout
        {
            get { return Timeout_; }
            set { Timeout_ = value; }
        }

        private int ReplyTime_;

        [Category("Time")]
        [DisplayName("Reply Time [ms]")]
        public int ReplyTime
        {
            get { return ReplyTime_; }
            set { ReplyTime_ = value; }
        }

        private S7CpuSeries CpuSeries_;

        [Category("Target")]
        [DisplayName("CPU Series")]
        public S7CpuSeries CpuSeries
        {
            get { return CpuSeries_; }
            set { CpuSeries_ = value; }
        }

        private int ReconnectAttempts_;

        [Category("Time")]
        [DisplayName("Reconnect Attempts")]
        public int ReconnectAttempts
        {
            get { return ReconnectAttempts_; }
            set { ReconnectAttempts_ = value < 1 ? 1 : value; }
        }

        private int ReadTimeout_;

        [Category("Time")]
        [DisplayName("Read Timeout [ms]")]
        public int ReadTimeout
        {
            get { return ReadTimeout_; }
            set { ReadTimeout_ = value < 1 ? 1 : value; }
        }

        private int WriteTimeout_;

        [Category("Time")]
        [DisplayName("Write Timeout [ms]")]
        public int WriteTimeout
        {
            get { return WriteTimeout_; }
            set { WriteTimeout_ = value < 1 ? 1 : value; }
        }

        public S7DriverParam()
        {
            Ip_ = IPAddress.Parse("127.0.0.1");
            Port_ = 102;
            Rack_ = 0;
            Slot_ = 2;
            Timeout_ = 1000000;
            ReplyTime_ = 1500;
            CpuSeries_ = S7CpuSeries.S7300;
            ReconnectAttempts_ = 3;
            ReadTimeout_ = 2000;
            WriteTimeout_ = 2000;
        }
    }
}