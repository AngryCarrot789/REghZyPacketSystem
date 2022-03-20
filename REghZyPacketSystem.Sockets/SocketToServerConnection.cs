using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using REghZy.Streams;
using REghZyPacketSystem.Exceptions;

namespace REghZyPacketSystem.Sockets {
    /// <summary>
    /// A reusable client connection. This will wait until the server has
    /// accepted a socket connection, and then allowing data to be transceived
    /// </summary>
    public class SocketToServerConnection : BaseConnection {
        private readonly Socket socket;
        private readonly EndPoint endPoint;
        private NetworkDataStream stream;
        private bool isConnected;

        /// <summary>
        /// The data stream which is linked to the server
        /// </summary>
        public override DataStream Stream => this.stream;

        /// <summary>
        /// Whether this client is connected to the server
        /// </summary>
        public override bool IsConnected => this.isConnected;

        /// <summary>
        /// The socket which links to the server
        /// </summary>
        public Socket Socket => this.socket;

        public EndPoint LocalEndPoint => this.socket.LocalEndPoint;

        public EndPoint RemoteEndPoint {
            get {
                if (this.socket.Connected) {
                    return this.socket.RemoteEndPoint;
                }
                else {
                    return this.endPoint;
                }
            }
        }

        /// <summary>
        /// Whether to use little endianness or big endianness (aka the order of bytes in big data types)
        /// </summary>
        public bool UseLittleEndianness { get; set; }

        public SocketToServerConnection(EndPoint endPoint, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp) {
            this.socket = new Socket(socketType, protocol);
            this.socket.SendTimeout = 30000;
            this.socket.ReceiveTimeout = 30000;
            this.endPoint = endPoint;
        }

        public SocketToServerConnection(IPAddress ip, int port, SocketType socketType = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp) {
            this.socket = new Socket(socketType, protocol);
            this.socket.SendTimeout = 30000;
            this.socket.ReceiveTimeout = 30000;
            this.endPoint = new IPEndPoint(ip, port);
        }

        /// <summary>
        ///
        /// </summary>
        /// <exception cref="ObjectDisposedException">The object is disposed</exception>
        /// <exception cref="ConnectionStatusException">The connection is already open</exception>
        /// <exception cref="ConnectionFailureException">Failed to open the connection</exception>
        /// <exception cref="IOException">An IO exception, most likely the network stream failed to open</exception>
        public override void Connect() {
            if (this.isDisposed) {
                throw new ObjectDisposedException("Cannot connect once the instance has been disposed!");
            }

            if (this.isConnected) {
                throw new ConnectionStatusException("Already connected!", true);
            }

            // this.server.ConnectWithTimeout(this.endPoint);
            try {
                this.socket.Connect(this.endPoint);
            }
            catch(Exception e) {
                this.socket.Close();
                throw new ConnectionFailureException($"Failed to connect to {this.endPoint}", e);
            }

            try {
                this.stream = CreateDataStream();
            }
            catch (Exception e) {
                throw new IOException("Failed to create network data stream", e);
            }

            this.isConnected = true;
        }

        /// <summary>
        /// Attempts to connect asynchronously
        /// </summary>
        /// <exception cref="ObjectDisposedException">The object is disposed</exception>
        /// <exception cref="ConnectionStatusException">The connection is already open</exception>
        /// <exception cref="ConnectionFailureException">Failed to open the connection</exception>
        /// <exception cref="IOException">An IO exception, most likely the network stream failed to open</exception>
        public async Task ConnectAsync() {
            if (this.isDisposed) {
                throw new ObjectDisposedException("Cannot connect once the instance has been disposed!");
            }

            if (this.isConnected) {
                throw new ConnectionStatusException("Already connected!", true);
            }

            try {
                await this.socket.ConnectAsync(this.endPoint);
            }
            catch(Exception e) {
                this.socket.Close();
                throw new ConnectionFailureException($"Failed to connect to {this.endPoint}", e);
            }

            try {
                this.stream = CreateDataStream();
            }
            catch (Exception e) {
                throw new IOException("Failed to create network data stream", e);
            }

            this.isConnected = true;
        }

        public override void Disconnect() {
            if (this.isDisposed) {
                throw new ObjectDisposedException("Cannot disconnect once the instance has been disposed!");
            }

            if (!this.isConnected) {
                throw new ConnectionStatusException("Already disconnected!", false);
            }

            try {
                this.socket.Disconnect(true);
            }
            catch (Exception e) {
                throw new IOException("Failed to disconnect socket from server", e);
            }

            this.stream.Dispose();
            this.stream = null;
            this.isConnected = false;
        }

        protected NetworkDataStream CreateDataStream() {
            if (this.UseLittleEndianness) {
                return NetworkDataStream.LittleEndianness(this.socket);
            }
            else {
                return NetworkDataStream.BigEndianness(this.socket);
            }
        }
    }
}
