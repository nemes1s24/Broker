using System;
using ProtoBuf;

namespace Client.Models
{
    [Serializable]
    [ProtoContract]
    internal struct Message
    {
        [ProtoMember(1)]
        public long SequenceNumber { get; set; }

        [ProtoMember(2)]
        public int Value { get; set; }
    }
}
