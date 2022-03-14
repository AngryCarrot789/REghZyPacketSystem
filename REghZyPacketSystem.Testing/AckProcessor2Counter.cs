using System;
using REghZyPacketSystem.Packeting.Ack;
using REghZyPacketSystem.Systems;

namespace REghZyPacketSystem.Testing {
    public class AckProcessor2Counter : AckProcessor<Packet2Counter> {
        public uint count;

        public AckProcessor2Counter(PacketSystem system) : base(system) {

        }

        protected override bool OnProcessPacketFromClient(Packet2Counter packet) {
            switch (packet.action) {
                case Packet2Counter.CountAction.incr: this.count += 200;
                    break;
                case Packet2Counter.CountAction.decr: this.count -= 200;
                    break;
            }

            SendToClient(packet, new Packet2Counter() {count = this.count});
            return true;
        }

        protected override void HandleResendPacket(Packet2Counter packet) {
            this.system.SendPacket(packet);
            Console.WriteLine($"Re-transmitting packet '{packet.GetType().Name}' with key {packet.key}");
        }
    }
}