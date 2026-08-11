using System;

namespace FenixServer.ViewModels
{
    public sealed class ConnectionRow
    {
        public Guid SourceId { get; set; }
        public string Kind { get; set; }
        public string Name { get; set; }
        public string Protocol { get; set; }
        public string Status { get; set; }
        public int Sent { get; set; }
        public int Received { get; set; }
    }
}