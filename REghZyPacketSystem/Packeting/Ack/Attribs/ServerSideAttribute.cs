using System;
using System.Diagnostics;

namespace REghZyPacketSystem.Packeting.Ack.Attribs {
    /// <summary>
    /// This is only used for server-based processing
    /// <para>
    /// Whether something is client side or server side isn't entirely dependent on which instance of the program is running,
    /// nor whether packets are sent or received. It depends mostly on what the purpose is.
    /// </para>
    /// <para>
    /// The server can be both a client and a server, so it depends on the perspective
    /// </para>
    /// <para>
    /// In ACK packets, the "client" sends a packet, the "server" receives it and sends a new packet back, and then the "client" receives it.
    /// But the "server" could also do this too. It could send a packet to the client, the client receives it and sends one back, and the server receives it.
    /// </para>
    /// <para>
    /// So this annotation should be used if the purpose is being the "sender" or "server", independent of what piece of software is running
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    [Conditional("DEBUG")]
    public class ServerSideAttribute : Attribute {

    }
}