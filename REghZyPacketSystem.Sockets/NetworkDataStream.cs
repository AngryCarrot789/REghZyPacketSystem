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

        public NetworkDataStream(Socket socket) : base(new NetworkStream(socket)) {
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
