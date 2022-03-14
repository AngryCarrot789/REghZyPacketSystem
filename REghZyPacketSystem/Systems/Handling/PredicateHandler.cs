using System;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Systems.Handling {
    public class PredicateHandler : IHandler {
        private readonly Predicate<Packet> handler;
        private readonly Predicate<Packet> canProcess;

        public PredicateHandler(Predicate<Packet> handler, Predicate<Packet> canProcess = null) {
            if (handler == null) {
                throw new NullReferenceException("Handler cannot be null");
            }

            this.handler = handler;
            this.canProcess = canProcess;
        }

        public bool CanProcess(Packet packet) {
            return this.canProcess == null ? true : this.canProcess(packet);
        }

        public bool Handle(Packet packet) {
            return this.handler(packet);
        }
    }
}
