using System;

namespace ProjectDataLib
{
    public class ScriptGlobals
    {
        public Project Project { get; set; }
        public Project Prj { get; set; }

        public dynamic GetTag(string name)
        {
            return Project?.GetTag(name);
        }

        public object SetTag(string name, object value)
        {
            return Project?.SetTag(name, value);
        }

        public void Write(object message)
        {
            Project?.Write(this, Convert.ToString(message));
        }
    }
}
