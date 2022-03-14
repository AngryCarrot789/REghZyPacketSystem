using System;
using System.Runtime.Serialization;

namespace REghZyPacketSystem.Exceptions {
    /// <summary>
    /// An exception used to indicate data loss, typically when reading packets
    /// </summary>
    public class DataLossException : PacketException {
        public DataLossException() {
        }

        protected DataLossException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        public DataLossException(string message) : base(message) {

        }

        public DataLossException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}