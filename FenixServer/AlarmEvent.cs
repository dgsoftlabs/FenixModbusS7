using System;

namespace FenixServer
{
    /// <summary>
    /// Klasa jednego alarmu
    /// </summary>
    public class AlarmEvent
    {
        public AlarmEvent(string Msg)
        {
            this.Mess = Msg;
            Tm = DateTime.Now;
        }

        public DateTime Tm { get; set; }
        public String Mess { get; set; }

        public string frDateTime
        { get { return Tm.ToShortDateString() + " " + Tm.ToShortTimeString(); } }
    }
}