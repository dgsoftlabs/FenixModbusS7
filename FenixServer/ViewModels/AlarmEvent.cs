using System;

namespace FenixServer.ViewModels
{
    public class AlarmEvent
    {
        public AlarmEvent(string msg)
        {
            Mess = msg;
            Tm = DateTime.Now;
        }

        public DateTime Tm { get; set; }

        public string Mess { get; set; }

        public string frDateTime => Tm.ToShortDateString() + " " + Tm.ToShortTimeString();
    }
}