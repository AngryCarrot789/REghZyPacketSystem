using System;
using System.Diagnostics;

namespace REghZyPacketSystem.Packeting.Ack.Attribs {
    /// <summary>
    /// This is used for both client and server side processing
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
    /// So this annotation should be used if the purpose is for both the "client" and "server" (e.g receiving any packet; the client and server both receive packets)
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    [Conditional("DEBUG")]
    public class BothSidesAttribute : Attribute {

    }
}