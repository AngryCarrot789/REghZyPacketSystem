using System.Net.Sockets;
using REghZy.Streams;

namespace REghZyPacketSystem.Sockets {
    /// <summary>
    /// A data stream that uses a <see cref="NetworkStream"/> as an underlying stream for reading/writing data
    /// </summary>
    public class NetworkDataStream : DataStream {
        private readonly Socket socket;

        public override long BytesAvailable => this.socket.Available;

        /// <summary>
        /// The network stream used by this data stream. This data stream's base stream uses this too
        /// </summary>
        public NetworkStream NetStream => (NetworkStream) base.Stream.Stream;

        /// <summary>
        /// The socket that this data stream uses
        /// </summary>
        public Socket Socket { get => this.socket; }

        /// <summary>
        /// A network data stream that uses the big endianness format for sending and receiving data
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static NetworkDataStream BigEndianness(Socket socket) {
            return new NetworkDataStream(socket);
        }

        /// <summary>
        /// A network data stream that uses the little endianness format for sending and receiving data
        /// </summary>
        public static NetworkDataStream LittleEndianness(Socket socket) {
            return new NetworkDataStream(socket, new DataInputStreamLE(), new DataOutputStreamLE());
        }

        public NetworkDataStream(Socket socket) : base(new NetworkStream(socket)) {
            this.socket = socket;
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        private NetworkDataStream(Socket socket, IDataInput input, IDataOutput output) : base(new NetworkStream(socket), input, output) {
            this.socket = socket;
            this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        public override bool CanRead() {
            // return this.networkStream.DataAvailable;
            // this is how the NetworkStream implements DataAvailable
            // this should save a good few clock cycles :-)
            return this.socket.Available != 0;
        }
    }
}
