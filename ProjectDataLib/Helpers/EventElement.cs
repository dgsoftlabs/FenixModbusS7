using System;

namespace ProjectDataLib
{
    public class EventElement
    {
        public EventElement(Project pr, object sender, byte[] d, DateTime t, string s, EventType type, EventElement el, string nm)
        {
            Name = nm;
            Sender = sender;
            Dane = d;
            Czas = t;
            Info = s;
            Type = type;
            Pr = pr;

            if (el != null)
            {
                RefTime = Convert.ToInt32((t - el.Czas).TotalMilliseconds);
            }
        }

        public int Index { get; set; }
        public string Name { get; set; }
        public object Sender { get; set; }
        public DateTime Czas { get; set; }
        public byte[] Dane { get; set; }
        public string Info { get; set; }
        public EventType Type { get; set; }
        public Project Pr { get; set; }
        public int RefTime { get; set; }
    }
}