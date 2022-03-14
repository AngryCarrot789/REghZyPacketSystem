using System;
using REghZyPacketSystem.Packeting;
using REghZyPacketSystem.Systems.Handling;

namespace REghZyPacketSystem.Exceptions {
    /// <summary>
    /// An exception that is thrown when a packet handler failed to handle a packet properly
    /// <para>
    /// The inner exception will be the exception that was thrown
    /// </para>
    /// </summary>
    public class PacketHandlerException : Exception {
        /// <summary>
        /// The packet that was involved (it could be modified since it was received)
        /// </summary>
        public Packet Packet { get; set; }

        /// <summary>
        /// Whether the handler was a listener. True if so, false if it was a handler
        /// </summary>
        public bool IsListener { get; set; }

        /// <summary>
        /// The priority of the handler/listener that threw the exception
        /// </summary>
        public Priority Priority { get; set; }

        public PacketHandlerException(Packet packet, Priority priority, bool isListener, Exception exception) 
            : base($"{(isListener ? "Listener" : "Handler")} (P={priority}) failed to handle packet type '{packet.GetType().Name}'", exception) {
            this.Packet = packet;
            this.IsListener = isListener;
            this.Priority = priority;
        }
    }
}
