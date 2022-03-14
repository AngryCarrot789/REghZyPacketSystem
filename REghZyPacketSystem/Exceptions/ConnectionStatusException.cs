using System;
using System.Runtime.Serialization;

namespace REghZyPacketSystem.Exceptions {
    public class ConnectionStatusException : Exception {
        public bool IsOpen { get; }

        public ConnectionStatusException(bool isOpen) {
            this.IsOpen = isOpen;
        }

        protected ConnectionStatusException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        public ConnectionStatusException(string message, bool isOpen) : base(message) {
            this.IsOpen = isOpen;
        }

        public ConnectionStatusException(string message, Exception innerException, bool isOpen) : base(message, innerException) {
            this.IsOpen = isOpen;
        }
    }
}