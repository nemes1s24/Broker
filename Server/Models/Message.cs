using System;
using ProtoBuf;

namespace Server.Models
{
    [Serializable]
    [ProtoContract]
    public struct Message
    {
        public Message(long sequenceNumber, int value)
        {
            SequenceNumber = sequenceNumber;
            Value = value;
        }

        [ProtoMember(1)]
        public long SequenceNumber { get; }

        [ProtoMember(2)]
        public int Value { get; }
    }
}
