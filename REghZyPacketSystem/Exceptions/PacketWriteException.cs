using System;
using System.Runtime.Serialization;

namespace REghZyPacketSystem.Exceptions {
    public class PacketWriteException : PacketException {
        public int WritesAttempted { get; set; }
        public int Written { get; set; }

        public PacketWriteException() {
        }

        public PacketWriteException(string message) : base(message) {
        }

        public PacketWriteException(string message, Exception innerException) : base(message, innerException) {
        }

        protected PacketWriteException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
