using System;
using System.Runtime.Serialization;

namespace REghZyPacketSystem.Exceptions {
    /// <summary>
    /// An exception to represent an error with a packet's payload
    /// </summary>
    public class PacketPayloadException : PacketException {
        public PacketPayloadException() {
        }

        protected PacketPayloadException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        public PacketPayloadException(string message) : base(message) {
        }

        public PacketPayloadException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}