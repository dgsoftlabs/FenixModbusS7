using System.Collections.Generic;

namespace FenixServer
{
    public class GraphElement
    {
        public string label;

        public GraphElement(string lab)
        {
            label = lab;
        }

        private List<object[]> dane = new List<object[]>();

        public List<object[]> data
        { get { return dane; } }

        public void addDataPoint(object[] d)
        {
            dane.Add(d);
        }

        public void removeElement(int nr)
        {
            if (dane.Count > 0)
                dane.RemoveAt(nr);
        }
    }
}