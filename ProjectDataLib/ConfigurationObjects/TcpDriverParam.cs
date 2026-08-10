using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;

namespace ProjectDataLib
{
    [Serializable]
    public class TcpDriverParam
    {
        private IPAddress Ip_;

        [Category("Target")]
        [DisplayName("Adress Ip")]
        public String Ip
        {
            get { return Ip_.ToString(); }
            set
            {
                try { Ip_ = IPAddress.Parse(value); }
                catch (Exception Ex) { Debug.WriteLine("[TcpDriverParam] Invalid IP: " + Ex.Message); }
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

        public TcpDriverParam()
        {
            Ip_ = IPAddress.Parse("127.0.0.1");
            Port_ = 502;
            Timeout_ = 1000;
            ReplyTime_ = 1500;
        }

        public override string ToString()
        {
            return " ";
        }
    }
}