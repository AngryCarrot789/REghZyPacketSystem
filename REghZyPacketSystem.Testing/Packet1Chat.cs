using REghZy.Streams;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Testing {
    [PacketImplementation(1)]
    public class Packet1Chat : Packet {
        public string msg;

        public override ushort GetPayloadSize() {
            return (ushort)this.msg.GetSizeUTF16WL();
        }

        public override void WritePayload(IDataOutput output) {
            output.WriteStringUTF16WL(this.msg);
        }

        public override void ReadPayLoad(IDataInput input, ushort length) {
            this.msg = input.ReadStringUTF16WL();
        }

        public override string ToString() {
            return $"{nameof(Packet1Chat)}(\"{this.msg ?? ""}\")";
        }
    }
}