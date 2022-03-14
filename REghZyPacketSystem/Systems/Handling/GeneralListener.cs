using System;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Systems.Handling {
    public class GeneralListener : IListener {
        private readonly Action<Packet> handler;

        public GeneralListener(Action<Packet> handler) {
            if (handler == null) {
                throw new NullReferenceException("Handler cannot be null");
            }

            this.handler = handler;
        }

        public void OnReceived(Packet packet) {
            this.handler(packet);
        }
    }
}
