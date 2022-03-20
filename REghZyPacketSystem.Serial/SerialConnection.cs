using System.IO.Ports;
using REghZy.Streams;

namespace REghZyPacketSystem.Serial {
/// <summary>
    /// A wrapper for a serial port, containing a data stream
    /// </summary>
    public class SerialConnection : BaseConnection {
        private SerialDataStream stream;
        private readonly SerialPort port;

        public override DataStream Stream => this.stream;

        public override bool IsConnected => this.port.IsOpen;

        /// <summary>
        /// The serial port that this serial connection uses to send/receive data
        /// </summary>
        public SerialPort Port => this.port;

        /// <summary>
        /// Whether to use little endianness or big endianness (aka the order of bytes in big data types)
        /// </summary>
        public bool UseLittleEndianness { get; set; }

        public SerialConnection(string port, int baud = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One) {
            this.port = new SerialPort(port, baud, parity, dataBits, stopBits);
            this.port.Handshake = Handshake.None;
            this.port.DiscardNull = false;
            // this.port.ErrorReceived += this.Port_ErrorReceived;
            // this.port.ReadTimeout = 10000;
            // this.port.WriteTimeout = 10000;
        }

        // private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
        //     switch (e.EventType) {
        //         case SerialError.TXFull:
        //             Console.WriteLine($"[SerialError] - TXFull: Write buffer was overridden");
        //             break;
        //         case SerialError.RXOver:
        //             Console.WriteLine($"[SerialError] - RXOver: Too much data received, cannot read quick enough!");
        //             break;
        //         case SerialError.Overrun:
        //             Console.WriteLine($"[SerialError] - Overrun: The last written character was overridden... packet loss!");
        //             break;
        //         case SerialError.RXParity:
        //             Console.WriteLine($"[SerialError] - RXParity: A parity error was detected!");
        //             break;
        //         case SerialError.Frame:
        //             Console.WriteLine($"[SerialError] - Frame: A framing error was detected!");
        //             break;
        //         default:
        //             break;
        //     }
        // }

        public override void Connect() {
            this.port.Open();
            this.port.DtrEnable = true;
            if (this.UseLittleEndianness) {
                this.stream = SerialDataStream.LittleEndianness(this.port);
            }
            else {
                this.stream = SerialDataStream.BigEndianness(this.port);
            }

            ClearBuffers();
        }

        public override void Disconnect() {
            this.port.DtrEnable = false;
            this.port.DiscardInBuffer();
            this.port.DiscardOutBuffer();
            this.port.Close();
            this.stream = null;
        }

        /// <summary>
        /// Clears the serial buffers
        /// </summary>
        public virtual void ClearBuffers() {
            this.port.DiscardInBuffer();
            this.port.DiscardOutBuffer();
        }

        public override void Dispose() {
            base.Dispose();
            if (this.port.IsOpen) {
                ClearBuffers();
                this.port.Close();
            }

            this.port.Dispose();
        }
    }
}