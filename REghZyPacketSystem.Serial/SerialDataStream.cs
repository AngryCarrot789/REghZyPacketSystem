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

        public SerialDataStream(SerialPort port) : base(port.BaseStream) {
            this.port = port;
        }

        public override bool CanRead() {
            return this.port.BytesToRead > 0;
        }
    }
}