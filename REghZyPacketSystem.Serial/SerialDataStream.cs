using System.IO.Ports;
using REghZy.Streams;

namespace REghZyPacketSystem.Serial {
    /// <summary>
    /// A data stream that uses the <see cref="SerialPort.BaseStream"/> for reading and writing data
    /// </summary>
    public class SerialDataStream : DataStream {
        private readonly SerialPort port;

        /// <summary>
        /// The serial port that this data stream uses
        /// </summary>
        public SerialPort Port { get => this.port; }

        public override long BytesAvailable => (long)this.port.BytesToRead;

        public static SerialDataStream BigEndianness(SerialPort port) {
            return new SerialDataStream(port, new DataInputStream(), new DataOutputStream());
        }

        public static SerialDataStream LittleEndianness(SerialPort port) {
            return new SerialDataStream(port, new DataInputStreamLE(), new DataOutputStreamLE());
        }

        public SerialDataStream(SerialPort port) : base(port.BaseStream) {
            this.port = port;
        }

        private SerialDataStream(SerialPort port, IDataInput input, IDataOutput output) : base(port.BaseStream, input, output) {
            this.port = port;
        }

        public override bool CanRead() {
            return this.port.BytesToRead > 0;
        }
    }
}