using System;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Systems.Handling {
    public class GenericHandler : IHandler {
        private readonly Type type;
        private readonly Predicate<Packet> handler;

        public GenericHandler(Type type, Predicate<Packet> handler) {
            if (handler == null) {
                throw new NullReferenceException("Handler cannot be null");
            }

            this.type = type;
            this.handler = handler;
        }

        public bool CanProcess(Packet packet) {
            return this.type.IsInstanceOfType(packet);
        }

        public bool Handle(Packet packet) {
            return this.handler(packet);
        }
    }

    public class GenericHandler<T> : IHandler where T : Packet {
        private readonly Predicate<T> handler;
        private readonly Predicate<T> canProcess;

        public GenericHandler(Predicate<T> handler) {
            if (handler == null) {
                throw new NullReferenceException("Handler cannot be null");
            }

            this.handler = handler;
            this.canProcess = null;
        }

        public GenericHandler(Predicate<T> handler, Predicate<T> canProcess) {
            if (handler == null) {
                throw new NullReferenceException("Handler cannot be null");
            }

            this.handler = handler;
            this.canProcess = canProcess;
        }

        public bool CanProcess(Packet packet) {
            return (this.canProcess == null && packet is T) || (packet is T pkt && this.canProcess(pkt));
        }

        public bool Handle(Packet packet) {
            return this.handler((T) packet);
        }
    }
}
