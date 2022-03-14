using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using REghZyPacketSystem.Packeting.Ack.Attribs;
using REghZyPacketSystem.Systems;
using REghZyPacketSystem.Systems.Handling;

namespace REghZyPacketSystem.Packeting.Ack {
    /// <summary>
    /// A helper class for processing ACK packets. Generally, there should only be 1 instance of the
    /// AckProcessor per packet type (each implementation is usually a singleton). At least, that's the intention
    /// </summary>
    /// <typeparam name="TPacket">The type of ACK packet that this processor will process</typeparam>
    public abstract class AckProcessor<TPacket> where TPacket : PacketACK {
        protected readonly PacketSystem system;

        // Caches packets that have been read/received
        [ClientSide] private readonly Dictionary<uint, TPacket> readCache;

        // Caches packets that have been sent (used for retransmission)
        [ClientSide] private readonly Dictionary<uint, TPacket> sendCache;

        [ServerSide] private readonly IdempotencyKeyStore usedKeys;
        [ServerSide] private bool allowDuplicateKey;
        [ClientSide] private bool allowResendPacket;
        [ClientSide] private long packetResendTime;
        [ClientSide] private uint nextId;
        [ClientSide] protected bool isRequestUnderWay;

        /// <summary>
        /// The packet system that this Ack processor uses to send and receive packets from
        /// </summary>
        public PacketSystem System => this.system;

        /// <summary>
        /// States whether packets will be re-sent if they aren't received after a specific amount of time (see <see cref="PacketResendTime"/>)
        /// </summary>
        [ClientSide]
        public bool AllowResendPacket {
            get => this.allowResendPacket;
            set => this.allowResendPacket = value;
        }

        /// <summary>
        /// The amount of time to wait before sending another packet, only if a responce isn't received within this time period (in milliseconds)
        /// </summary>
        [ClientSide]
        public long PacketResendTime {
            get => this.packetResendTime;
            set => this.packetResendTime = value;
        }

        /// <summary>
        /// States whether to process packets that use a key that's already been processed
        /// <para>
        /// This is false by default
        /// </para>
        /// </summary>
        [ServerSide]
        public bool AllowDuplicatedKey {
            get => this.allowDuplicateKey;
            set => this.allowDuplicateKey = value;
        }

        /// <summary>
        /// Creates a new AckProcessor
        /// </summary>
        /// <param name="system">The packet system that this Ack processor uses to send and receive packets from</param>
        /// <param name="priority">The priority of the packet handler</param>
        /// <exception cref="ArgumentNullException">The packet system is null</exception>
        protected AckProcessor(PacketSystem system, Priority priority = Priority.HIGHEST) {
            if (system == null) {
                throw new ArgumentNullException(nameof(system), "Network cannot be null");
            }

            this.readCache = new Dictionary<uint, TPacket>();
            this.sendCache = new Dictionary<uint, TPacket>();
            this.system = system;
            this.isRequestUnderWay = false;
            this.allowDuplicateKey = false;
            this.allowResendPacket = true;
            this.packetResendTime = 1000;
            this.usedKeys = new IdempotencyKeyStore();
            this.nextId = 0;
            system.RegisterHandler<TPacket>(OnPacketReceived, priority);
        }

        [ClientSide]
        protected uint GetNextKey() {
            return ++this.nextId;
        }

        /// <summary>
        /// Sets up the given packet's key and destination for you, and sends the packet, returning the ID of the packet
        /// <para>
        /// This packet is now participating in the ACK transaction, therefore, no other packets should be sent until
        /// the responce has been received (aka a packet, that this processor processes,
        /// is received in direction <see cref="Destination.ToClient"/>)
        /// </para>
        /// </summary>
        [ClientSide]
        public uint SendRequest(TPacket packet) {
            if (this.isRequestUnderWay) {
                throw new Exception("Concurrent ACK request! A packet is already in transit");
            }

            this.isRequestUnderWay = true;
            packet.key = GetNextKey();
            packet.destination = Destination.ToServer;
            this.sendCache[packet.key] = packet;
            this.system.SendPacket(packet);
            return packet.key;
        }

        /// <summary>
        /// Asynchronously waits until an ACK packet (of this processor's generic type) is received and whose key is the given key.
        /// <para>
        /// See <see cref="AllowResendPacket"/> to allow re-sending packets if they aren't received after <see cref="PacketResendTime"/>
        /// </para>
        /// </summary>
        /// <param name="key">
        /// The key that the received ACK packet (of this processor's generic
        /// type) must have, in order for this method to return
        /// </param>
        /// <returns>
        /// The packet that the server has sent
        /// </returns>
        [ClientSide]
        public async Task<TPacket> ReceiveResponceAsync(uint key) {
            // avoid constant ldfld opcode, just in case it doesn't get optimised
            Dictionary<uint, TPacket> dictionary = this.readCache;
            bool allowResend = this.allowResendPacket;
            long start = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
            int count = 0;

            while (true) {
                if (dictionary.TryGetValue(key, out TPacket packet)) {
                    dictionary.Remove(key);
                    this.sendCache.Remove(key);
                    return packet;
                }

                if (allowResend) {
                    if (++count > 10) {
                        count = 0;
                        long current = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;
                        if ((current - start) > this.packetResendTime) {
                            start = current;
                            if (this.sendCache.TryGetValue(key, out TPacket pkt)) {
                                HandleResendPacket(pkt);
                            }
                        }
                    }
                }

                await Task.Delay(2);
            }
        }

        /// <summary>
        /// Sends a request and awaits its responce asynchronously
        /// </summary>
        /// <param name="packet">The packet to send to the server</param>
        /// <returns>
        /// The packet sent back from the server
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ClientSide]
        public async Task<TPacket> MakeRequestAsync(TPacket packet) {
            return await ReceiveResponceAsync(SendRequest(packet));
        }

        /// <summary>
        /// A helper function for sending a packet back to the client. It automatically sets the key from <paramref name="fromClient"/>,
        /// and sets the destination code to <see cref="Destination.ToClient"/>.
        /// <para>
        /// This method just sets the key, and then invokes <see cref="SendToClient(TPacket)"/>
        /// </para>
        /// </summary>
        /// <param name="fromClient">The original packet received from the client</param>
        /// <param name="toClient">The new packet that contains custom response data</param>
        [ServerSide]
        protected void SendToClient(TPacket fromClient, TPacket toClient) {
            toClient.key = fromClient.key;
            SendToClient(toClient);
        }

        /// <summary>
        /// A helper function for sending a packet back to the client. It automatically sets the destination for you,
        /// and then adds the packet to the send packet cache (in case a request is made with the same ID),
        /// and then sends the given packet again (with the destination code of <see cref="Destination.ToClient"/>)
        /// </summary>
        /// <param name="packet">The packet to send to the client</param>
        [ServerSide]
        protected void SendToClient(TPacket packet) {
            packet.destination = Destination.ToClient;
            this.system.SendPacket(packet);
        }

        [BothSides]
        private bool OnPacketReceived(TPacket packet) {
            Destination destination = packet.destination;
            if (destination == Destination.Ack) {
                uint key = packet.key;
                if (this.usedKeys.HasKey(key)) {
                    if (!HandleRepeatIdempotency(packet)) {
                        return true;
                    }
                }

                if (OnProcessPacketFromClient(packet)) {
                    this.usedKeys.Put(key);
                    return true;
                }
                else {
                    return false;
                }
            }
            else if (destination == Destination.ToClient) {
                if (OnProcessPacketFromServer(packet)) {
                    this.isRequestUnderWay = false;
                    this.readCache[packet.key] = packet;
                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                throw new Exception("Unexpected destination code: " + destination);
            }
        }

        /// <summary>
        /// Re-sends the request to the server, usually used if a response not being received
        /// <para>
        /// This can be overridden to remove the retransmission functionality (although setting <see cref="AllowResendPacket"/> would be easier),
        /// or for altering how packets are re-sent (or removing the console logging)
        /// </para>
        /// </summary>
        /// <param name="packet">
        /// The packet that was originally sent to the server (the one that was created client side)
        /// </param>
        [ClientSide]
        protected virtual void HandleResendPacket(TPacket packet) {
            this.system.SendPacket(packet);
            // Console.WriteLine($"Re-transmitting packet '{packet.GetType().Name}'");
        }

        /// <summary>
        /// Called when a packet is received with an idempotency key that was already processed
        /// <para>
        /// The default is to return <see cref="AllowDuplicatedKey"/>, which defaults to false
        /// </para>
        /// </summary>
        /// <param name="packet">
        /// The packet (from the client) containing the repeated idempotency key
        /// </param>
        /// <returns>
        /// Whether to continue handling the packet. If false, then stop processing the packet. Otherwise, process it.
        /// This method just specified what to actually do when a repeated key is received.
        /// Usually, this method just returns <see cref="AllowDuplicatedKey"/>. That property isn't checked before this method runs,
        /// so it's up to this method to check it; it could be true or false
        /// </returns>
        [ServerSide]
        protected virtual bool HandleRepeatIdempotency(TPacket packet) {
            if (this.allowDuplicateKey) {
                return true;
            }

            // Console.WriteLine($"Duplicated idempotency key '{packet.key}' for packet '{packet.GetType().Name}'");
            return false;
        }

        /// <summary>
        /// This will be called if we are the server. It is called when we receive a packet from the client,
        /// aka the mid-way between getting and receiving data (that is, if the ACK packet is used for that)
        /// <para>
        /// If the client wanted data (which is usually the usage for ACK packets), the packet in the parameters  will usually
        /// contain request information, which will be used to fill in data for a new packet, and then that one gets sent to the client
        /// </para>
        /// <para>
        /// By default, this should return true, unless some explicit functionality is required
        /// </para>
        /// </summary>
        /// <returns>
        /// True if the packet is fully handled, and shouldn't be sent anywhere else (see <see cref="IHandler.Handle(Packet)"/>),
        /// otherwise false if the packet shouldn't be handled, and can possibly be sent to other handlers/listeners
        /// </returns>
        [ServerSide]
        protected abstract bool OnProcessPacketFromClient(TPacket packet);

        /// <summary>
        /// This will only be called if this is the client. It is called when we (the client)
        /// receive a packet from the server (usually after the server runs <see cref="OnProcessPacketFromClient"/>)
        /// <para>
        /// If the client wanted data (which is usually the usage for ACK packets),
        /// the packet in the parameters will usually contain the information requested
        /// </para>
        /// <para>
        /// Usually, this method is empty, because you usually use the <see cref="ReceiveResponceAsync"/>
        /// method. But that method relies on <see cref="readCache"/> containing the packet, and the packet is
        /// only placed in there once this method returns <see langword="true"/>
        /// </para>
        /// <para>
        /// And if this returns <see langword="false"/>, then <see cref="ReceiveResponceAsync"/> will never return,
        /// essentially meaning you completely ignore the packet (although listeners
        /// that use <see cref="Priority.HIGHEST"/> will be able to sniff it)
        /// </para>
        /// </summary>
        [ClientSide]
        protected virtual bool OnProcessPacketFromServer(TPacket packet) {
            return true;
        }
    }
}