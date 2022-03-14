using System;
using System.Runtime.Serialization;

namespace REghZyPacketSystem.Exceptions {
    /// <summary>
    /// Thrown when the creation of a packet failed
    /// </summary>
    public class PacketCreationException : PacketException {
        public PacketCreationException() {
        }

        protected PacketCreationException(SerializationInfo info, StreamingContext context) : base(info, context) {

        }

        public PacketCreationException(string message) : base(message) {

        }

        public PacketCreationException(string message, Exception innerException) : base(message, innerException) {

        }
    }
}