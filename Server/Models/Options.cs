using System;

namespace Server.Models
{
    [Serializable]
    public class Options
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
    }
}
