using System;

namespace Client.Models
{
    [Serializable]
    public class Options
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public int HoldingDuration { get; set; }
    }
}
