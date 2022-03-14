using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace REghZyPacketSystem.Sockets {
    /// <summary>
    /// Set the scene: My computer, and an arduino. Even though an arduino can't run C# code... still
    /// <para>
    /// The arduino is the server, and it invokes <see cref="AcceptClientConnection(Socket)"/>. This will wait
    /// until my computer has tried to connect to it, and once it has, it begins sending/receiving packets
    /// </para>
    /// <para>
    /// My computer makes a connection to the arduino by calling <see cref="MakeConnectionToServer(EndPoint)"/>. It sits
    /// there and waits until the arduino accepts it (it will call <see cref="AcceptClientConnection(Socket)"/>). And
    /// then it begins sending/receiving packets
    /// </para>
    /// <para>
    /// The reason the arduino is the server, is because it just is because i want it to be :-) and its easier
    /// </para>
    /// <para>
    /// Whereas, my computer, it only needs that arduino connection, and no other connections
    /// </para>
    /// </summary>
    public static class SocketHelper {
        /// <summary>
        /// This is typically used server side, to create the listener socket for accepting clients
        /// </summary>
        /// <param name="localEndPoint">The end point that this server is located at</param>
        /// <param name="addressFamily">The address family</param>
        /// <param name="socketType">
        /// The type of socket this will be. This should usually be
        /// a stream, because the connections are built around streams
        /// </param>
        /// <param name="protocolType">The protocol to use (e.g tcp, udp, etc)</param>
        /// <returns>
        /// The socket that is pre-bound to the given local end point (no need to call <see cref="Socket.Bind(EndPoint)"/>)
        /// </returns>
        public static Socket CreateServerSocket(EndPoint localEndPoint, AddressFamily addressFamily, SocketType socketType = SocketType.Stream, ProtocolType protocolType = ProtocolType.Tcp) {
            Socket socket = new Socket(addressFamily, socketType, protocolType);
            socket.Bind(localEndPoint);
            // socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.UseLoopback, true);
            return socket;
        }

        /// <summary>
        /// This is typically used server side, to create the listener socket for accepting clients
        /// </summary>
        /// <param name="localEndPoint">The end point that this server is located at</param>
        /// <param name="addressFamily">The address family</param>
        /// <param name="socketType">
        /// The type of socket this will be. This should usually be
        /// a stream, because the connections are built around streams
        /// </param>
        /// <param name="protocolType">The protocol to use (e.g tcp, udp, etc)</param>
        /// <returns>
        /// The socket that is pre-bound to the given local end point (no need to call <see cref="Socket.Bind(EndPoint)"/>)
        /// </returns>
        public static Socket CreateServerSocket(EndPoint localEndPoint, SocketType socketType = SocketType.Stream, ProtocolType protocolType = ProtocolType.Tcp) {
            Socket socket = new Socket(socketType, protocolType);
            socket.Bind(localEndPoint);
            // socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.UseLoopback, true);
            return socket;
        }

        /// <summary>
        /// Creates a socket (with the given socket type and protocol) and binds it to the given IP address and the given port
        /// </summary>
        /// <param name="address">The local IP address to bind to</param>
        /// <param name="port">The port to use</param>
        /// <param name="socketType">The socket type to use (usually should be stream, because that's what the packet systems use)</param>
        /// <param name="protocolType">The protocol to use (TCP recommended, otherwise packet loss is probable)</param>
        /// <returns>
        /// A socket that is bound to the given port on the given IP. It is not in a listening state, that must be done manually
        /// </returns>
        public static Socket CreateServerSocket(IPAddress address, int port, SocketType socketType = SocketType.Stream, ProtocolType protocolType = ProtocolType.Tcp) {
            Socket socket = new Socket(socketType, protocolType);
            socket.Bind(new IPEndPoint(address, port));
            return socket;
        }

        /// <summary>
        /// Creates a socket (with the given socket type and protocol) and binds it to <see cref="IPAddress.Any"/> and the given port
        /// </summary>
        /// <param name="port">The port to use</param>
        /// <param name="socketType">The socket type to use (usually should be stream, because that's what the packet systems use)</param>
        /// <param name="protocolType">The protocol to use (TCP recommended, otherwise packet loss is probable)</param>
        /// <returns>
        /// A socket that is bound to the given port on <see cref="IPAddress.Any"/>. It is not in a listening state, that must be done manually
        /// </returns>
        public static Socket CreateServerSocket(int port, SocketType socketType = SocketType.Stream, ProtocolType protocolType = ProtocolType.Tcp) {
            Socket socket = new Socket(socketType, protocolType);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            return socket;
        }

        // /// <summary>
        // /// Uses <see cref="Socket.BeginConnect(EndPoint, AsyncCallback, object)"/> to start a
        // /// connection, and waits the given number of millis to connect
        // /// </summary>
        // /// <param name="socket"></param>
        // /// <param name="endPoint"></param>
        // /// <param name="timeoutMillis"></param>
        // /// <exception cref="Exception"></exception>
        // public static void ConnectWithTimeout(this Socket socket, EndPoint endPoint, int timeoutMillis = -1) {
        //     IAsyncResult result = socket.BeginConnect(endPoint, null, null);
        //     result.AsyncWaitHandle.WaitOne(timeoutMillis, true);
        //     if (socket.Connected) {
        //         socket.EndConnect(result);
        //     }
        //     else {
        //         socket.Close();
        //         throw new Exception("Failed to connect server, took longer than " + timeoutMillis + " to connect");
        //     }
        // }

        /// <summary>
        /// We are the client, and we want to make a connection to the server
        /// <para>
        /// You don't need to call <see cref="BaseConnection.Connect"/>, it will be done automatically in this method
        /// </para>
        /// </summary>
        /// <returns>
        /// A connection that is already connected
        /// </returns>
        public static SocketToServerConnection MakeConnectionToServer(EndPoint serverEndPoint) {
            SocketToServerConnection connection = new SocketToServerConnection(serverEndPoint);
            connection.Connect();
            return connection;
        }

        /// <summary>
        /// We are the client, and we want to make a connection to the server
        /// <para>
        /// You don't need to call <see cref="BaseConnection.Connect"/>, it will be done automatically in this method
        /// </para>
        /// </summary>
        /// <returns>
        /// A connection that is already connected
        /// </returns>
        public static async Task<SocketToServerConnection> MakeConnectionToServerAsync(EndPoint serverEndPoint) {
            SocketToServerConnection connection = new SocketToServerConnection(serverEndPoint);
            await connection.ConnectAsync();
            return connection;
        }

        /// <summary>
        /// We are the client, and we want to make a connection to the server
        /// <para>
        /// You don't need to call <see cref="BaseConnection.Connect"/>, it will be done automatically in this method
        /// </para>
        /// </summary>
        /// <returns>
        /// A connection that is already connected
        /// </returns>
        public static SocketToServerConnection MakeConnectionToServer(IPAddress ip, int port) {
            SocketToServerConnection connection = new SocketToServerConnection(ip, port);
            connection.Connect();
            return connection;
        }

        /// <summary>
        /// We are the client, and we want to make a connection to the server
        /// <para>
        /// You don't need to call <see cref="BaseConnection.Connect"/>, it will be done automatically in this method
        /// </para>
        /// </summary>
        /// <returns>
        /// A connection that is already connected
        /// </returns>
        public static async Task<SocketToServerConnection> MakeConnectionToServerAsync(IPAddress ip, int port) {
            SocketToServerConnection connection = new SocketToServerConnection(ip, port);
            await connection.ConnectAsync();
            return connection;
        }

        /// <summary>
        /// We are the server, and we want to accept any incomming connection from clients
        /// <para>
        /// You don't need to call <see cref="BaseConnection.Connect"/> on the packet network that
        /// this method returns, it won't do anything. See <see cref="SocketToClientConnection"/>;
        /// it's a one-time connection, you must create a new instance to have a new connection
        /// </para>
        /// </summary>
        /// <param name="server">The server connection</param>
        /// <returns>
        /// A connection that is connected to client
        /// </returns>
        public static SocketToClientConnection AcceptClientConnection(Socket server) {
            Socket client = server.Accept();
            return new SocketToClientConnection(client, server);
        }

        /// <summary>
        /// We are the server, and we want to accept any incomming connection from clients
        /// <para>
        /// You don't need to call <see cref="BaseConnection.Connect"/> on the packet network that
        /// this method returns, it won't do anything. See <see cref="SocketToClientConnection"/>;
        /// it's a one-time connection, you must create a new instance to have a new connection
        /// </para>
        /// </summary>
        /// <param name="server">The server connection</param>
        /// <returns>
        /// A connection that is connected to client
        /// </returns>
        public static async Task<SocketToClientConnection> AcceptClientConnectionAsync(Socket server) {
            Socket client = await server.AcceptAsync();
            return new SocketToClientConnection(client, server);
        }
    }
}
