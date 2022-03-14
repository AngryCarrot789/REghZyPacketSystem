using REghZy.Streams;
using REghZyPacketSystem.Packeting;
using REghZyPacketSystem.Packeting.Ack;

namespace REghZyPacketSystem.Testing {
    [PacketImplementation(2)]
    public class Packet2Counter : PacketACK {
        public enum CountAction : byte {
            incr,
            decr,
            getvalue
        }

        public CountAction action;
        public uint count;

        public override ushort GetPayloadSizeToServer() {
            return 1; // action
        }

        public override ushort GetPayloadSizeToClient() {
            return 4; // count
        }

        public override void ReadPayloadFromClient(IDataInput input, ushort length) {
            this.action = (CountAction) input.ReadByte(); // read action
        }

        public override void ReadPayloadFromServer(IDataInput input, ushort length) {
            this.count = input.ReadUInt(); // read count
            // if (this.count == 2) {
            //     throw new System.Exception("Value cannot be 2!!!");
            // }
        }

        public override void WritePayloadToServer(IDataOutput output) {
            output.WriteEnum8(this.action);
        }

        public override void WritePayloadToClient(IDataOutput output) {
            output.WriteUInt(this.count);
        }

        public override string ToString() {
            return $"{nameof(Packet2Counter)}({this.key} -> {this.destination}: {this.action} -> {this.count})";
        }
    }
}