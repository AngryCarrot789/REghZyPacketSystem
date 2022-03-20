using System;
using System.Collections.Generic;
using System.Threading;
using REghZy.Streams;
using REghZyPacketSystem.Exceptions;
using REghZyPacketSystem.Packeting;
using REghZyPacketSystem.Systems.Handling;

namespace REghZyPacketSystem.Systems {
    /// <summary>
    /// The packet network is what handles sending and receiving packets, and delivering received packets to listeners
    /// <para>
    /// At it's base level, it's just a wrapper for reading and writing packets from
    /// a <see cref="DataStream"/> (by holding packets in queues). It also contains 
    /// a map of handlers and listeners too
    /// </para>
    /// </summary>
    public class PacketSystem {
        private readonly ListenerMap map;
        protected readonly BaseConnection connection;

        /// <summary>
        /// The packets that have been read/received from the connection, and are ready to be processed
        /// </summary>
        protected readonly Queue<Packet> readQueue;

        /// <summary>
        /// The packets that have been created, and are ready to be sent to the connection
        /// </summary>
        protected readonly Queue<Packet> sendQueue;

        /// <summary>
        /// The packets that have been read/received from the connection, and are ready to be processed
        /// </summary>
        public Queue<Packet> ReadQueue { get => this.readQueue; }

        /// <summary>
        /// The packets that have been created, and are ready to be sent to the connection
        /// </summary>
        public Queue<Packet> SendQueue { get => this.sendQueue; }

        /// <summary>
        /// The connection that this packet system uses
        /// </summary>
        public BaseConnection Connection => this.connection;

        /// <summary>
        /// Indicates whether this packet's connection is connected or not
        /// <para>
        /// Calling <see cref="Start"/> should result in this being <see langword="true"/>
        /// </para>
        /// <para>
        /// Calling <see cref="Stop"/> should result in this being <see langword="false"/>
        /// </para>
        /// </summary>
        public bool IsConnected => this.connection.IsConnected;

        /// <summary>
        /// Creates a new instance of a packet system, using the given connection
        /// </summary>
        public PacketSystem(BaseConnection connection) {
            if (connection == null) {
                throw new ArgumentNullException(nameof(connection), "Connection cannot be null");
            }

            this.connection = connection;
            this.readQueue = new Queue<Packet>();
            this.sendQueue = new Queue<Packet>();
            this.map = new ListenerMap();
        }

        /// <summary>
        /// Registers a plain predicate packet handler
        /// </summary>
        public IHandler RegisterHandler(PredicateHandler handler, Priority priority = Priority.NORMAL) {
            return this.map.AddHandler(priority, handler);
        }

        /// <summary>
        /// Registers the given predicate as a <see cref="PredicateHandler"/>
        /// </summary>
        public IHandler RegisterHandler(Predicate<Packet> handler, Priority priority = Priority.NORMAL) {
            return this.map.AddHandler(priority, new PredicateHandler(handler));
        }

        /// <summary>
        /// Registers the given predicate as a <see cref="PredicateHandler"/>, where the canProcess parameter
        /// will be used to determine if the handler will receive packets or not
        /// </summary>
        /// <param name="canProcess">The predicate that decides if the handler can handle packets or not (therefore determining if handler will be invoked or not)</param>
        public IHandler RegisterHandler(Predicate<Packet> handler, Predicate<Packet> canProcess, Priority priority = Priority.NORMAL) {
            return this.map.AddHandler(priority, new PredicateHandler(handler, canProcess));
        }

        /// <summary>
        /// Registers the given predicate as a <see cref="PredicateHandler"/>
        /// <para>
        /// This predicate also provide the packet system which is dealing with the packet
        /// </para>
        /// </summary>
        public IHandler RegisterHandler(Func<PacketSystem, Packet, bool> handler, Priority priority = Priority.NORMAL) {
            return this.map.AddHandler(priority, new PredicateHandler((p) => handler(this, p)));
        }

        /// <summary>
        /// Registers the given predicate function as a <see cref="PredicateHandler"/>, where the canProcess parameter
        /// will be used to determine if the handler will receive packets or not
        /// <para>
        /// These two predicates also provide the packet system which is dealing with the packet
        /// </para>
        /// </summary>
        /// <param name="canProcess">The predicate that decides if the handler can handle packets or not (therefore determining if handler will be invoked or not)</param>
        public IHandler RegisterHandler(Func<PacketSystem, Packet, bool> handler, Func<PacketSystem, Packet, bool> canProcess, Priority priority = Priority.NORMAL) {
            return this.map.AddHandler(priority, new PredicateHandler((p) => handler(this, p), (p) => canProcess(this, p)));
        }

        /// <summary>
        /// Registers the given generic predicate handler
        /// </summary>
        /// <typeparam name="T">The packet type</typeparam>
        public IHandler RegisterHandler<T>(Predicate<T> handler, Priority priority = Priority.NORMAL) where T : Packet {
            return this.map.AddHandler(priority, new GenericHandler<T>(handler));
        }

        /// <summary>
        /// Registers the given predicate function as a <see cref="PredicateHandler"/>, where the canProcess parameter
        /// will be used to determine if the handler will receive packets or not
        /// </summary>
        /// <param name="canProcess">The predicate that decides if the handler can handle packets or not (therefore determining if handler will be invoked or not)</param>
        /// <typeparam name="T">The packet type</typeparam>
        public IHandler RegisterHandler<T>(Predicate<T> handler, Predicate<T> canProcess, Priority priority = Priority.NORMAL) where T : Packet {
            return this.map.AddHandler(priority, new GenericHandler<T>(handler, canProcess));
        }

        /// <summary>
        /// Registers the given predicate as a <see cref="PredicateHandler"/>
        /// <para>
        /// This predicate also provide the packet system which is dealing with the packet
        /// </para>
        /// </summary>
        /// <typeparam name="T">The packet type</typeparam>
        public IHandler RegisterHandler<T>(Func<PacketSystem, T, bool> handler, Priority priority = Priority.NORMAL) where T : Packet {
            return this.map.AddHandler(priority, new GenericHandler<T>((p) => handler(this, p)));
        }

        /// <summary>
        /// Registers the given predicate function as a <see cref="PredicateHandler"/>, where the canProcess parameter
        /// will be used to determine if the handler will receive packets or not
        /// <para>
        /// These two predicates also provide the packet system which is dealing with the packet
        /// </para>
        /// </summary>
        /// <typeparam name="T">The packet type</typeparam>
        public IHandler RegisterHandler<T>(Func<PacketSystem, T, bool> handler, Func<PacketSystem, T, bool> canProcess, Priority priority = Priority.NORMAL) where T : Packet {
            return this.map.AddHandler(priority, new GenericHandler<T>((p) => handler(this, p), (p) => canProcess(this, p)));
        }

        /// <summary>
        /// Registers the given plain packet listeners
        /// </summary>
        public IListener RegisterListener(GeneralListener handler, Priority priority = Priority.NORMAL) {
            return this.map.AddListener(priority, handler);
        }

        /// <summary>
        /// Registers the given callback as a <see cref="GeneralListener"/>
        /// </summary>
        public IListener RegisterListener(Action<Packet> handler, Priority priority = Priority.NORMAL) {
            return this.map.AddListener(priority, new GeneralListener(handler));
        }

        /// <summary>
        /// Registers the given callback as a <see cref="GeneralListener"/>, also providing the packet system that is dealing
        /// with the handler, which is useful in case anonymous methods aren't used and a general method is used instead
        /// </summary>
        public IListener RegisterListener(Action<PacketSystem, Packet> handler, Priority priority = Priority.NORMAL) {
            return this.map.AddListener(priority, new GeneralListener((p) => handler(this, p)));
        }

        /// <summary>
        /// Registers the given callback as a <see cref="GeneralListener"/>
        /// </summary>
        /// <typeparam name="T">The packet type</typeparam>
        public IListener RegisterListener<T>(Action<T> handler, Priority priority = Priority.NORMAL) where T : Packet {
            return this.map.AddListener(priority, new GenericListener<T>(handler));
        }

        /// <summary>
        /// Registers the given callback as a <see cref="GeneralListener"/>, also providing the packet system that is dealing
        /// with the handler, which is useful in case anonymous methods aren't used and a general method is used instead
        /// </summary>
        /// <typeparam name="T">The packet type</typeparam>
        public IListener RegisterListener<T>(Action<PacketSystem, T> handler, Priority priority = Priority.NORMAL) where T : Packet {
            return this.map.AddListener(priority, new GenericListener<T>((p) => handler(this, p)));
        }

        /// <summary>
        /// Unregisters a handler, stopping it from handling packets
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public bool UnregisterHandler(Priority priority, IHandler handler) {
            return this.map.GetHandlers(priority).Remove(handler);
        }

        /// <summary>
        /// Unregisters a listener, stopping it from listening to packets
        /// </summary>
        /// <returns></returns>
        public bool UnregisterListener(Priority priority, IListener handler) {
            return this.map.GetListeners(priority).Remove(handler);
        }

        /// <summary>
        /// Unregisters all listeners and handlers that this packet system uses
        /// </summary>
        public void ClearListenerAndHandlers() {
            this.map.Clear();
        }

        /// <summary>
        /// Reads the next available packet, and enqueues it in <see cref="ReadQueue"/>
        /// </summary>
        /// <returns>
        /// True if a packet was read, otherwise false (if there wasn't enough data available to read a packet header)
        /// </returns>
        public bool ReadNextPacket() {
            if (this.connection == null) {
                throw new NullReferenceException("Connection is unavailable");
            }

            long available = this.connection.Stream.BytesAvailable;
            if (available < Packet.MinimumHeaderSize)
                return false;

            lock (this.readQueue) {
                ReadNextPacketInternal(this.connection.Stream.Input);
            }

            return true;
        }

        internal void ReadNextPacketInternal(IDataInput input) {
            Packet packet;
            try {
                packet = Packet.ReadPacket(input);
            }
            catch (Exception e) {
                throw new PacketCreationException("Failed to read next packet", e);
            }

            this.readQueue.Enqueue(packet);
        }

        /// <summary>
        /// Tries to read a maximum of the given number of packets, unless there isn't enough data to read
        /// </summary>
        /// <returns>
        /// The number of packets that were queued in <see cref="ReadQueue"/>
        /// </returns>
        public int ReadNextPackets(int count = 10) {
            if (this.connection == null) {
                throw new NullReferenceException("Connection is unavailable");
            }

            int read = 0;
            lock (this.readQueue) {
                IDataInput input = this.connection.Stream.Input;
                while (this.connection.Stream.BytesAvailable >= Packet.MinimumHeaderSize) {
                    ReadNextPacketInternal(input);
                    if ((++read) == count) {
                        return read;
                    }
                }
            }

            return read;
        }

        /// <summary>
        /// Processes/handles the given number of packets that are currently queued in <see cref="ReadQueue"/>
        /// <para>
        /// If the packet system is used with WPF, or in a non-thread-safe environment, this method should
        /// only be run on the safe thread (e.g WPF's main thread/GUI thread)
        /// </para>
        /// </summary>
        /// <param name="count">The number of packets to try and handle</param>
        /// <returns>
        /// The number of packets that were handled. This may not be equal to the
        /// given number of packets (e.g there weren't enough packets in the queue)
        /// </returns>
        public int ProcessReadQueue(int count = 10) {
            lock (this.readQueue) {
                Queue<Packet> queue = this.readQueue;
                count = Math.Min(queue.Count, count);
                if (count <= 0) {
                    return 0;
                }

                int i = 0;
                ListenerMap handler = this.map;
                while (i != count) {
                    Packet packet = queue.Dequeue();
                    i++; // increment here, so if there's a failure on the 1st packet, it says "Failed 1/3" (if count == 3)
                    try {
                        handler.DeliverPacket(packet);
                    }
                    catch (PacketHandlerException e) {
                        throw new PacketException($"Failed to handle packet {i}/{count} (id {Packet.GetPacketID(packet)})", e);
                    }
                }

                return i;
            }
        }

        /// <summary>
        /// <para>
        /// If using a <see cref="ThreadPacketSystem"/>, this will be invoked by the write thread, so manually
        /// calling it isn't strictly necessary. Otherwise, see <see cref="SendPacketImmidiately"/>
        /// </para>
        /// </summary>
        /// <param name="count">The number of packets to try and write</param>
        /// <exception cref="PacketWriteException"></exception>
        /// <returns>
        /// The number of packets that were handled. This may not be equal to the given number of packets
        /// </returns>
        public int ProcessSendQueue(int count = 5) {
            if (this.connection == null) {
                throw new NullReferenceException("Connection is unavailable");
            }

            lock (this.sendQueue) {
                Queue<Packet> queue = this.sendQueue;
                count = Math.Min(queue.Count, count);
                if (count <= 0) {
                    return 0;
                }

                int sent = 0;
                IDataOutput output = this.connection.Stream.Output;
                while (sent != count) {
                    Packet packet = queue.Dequeue();
                    sent++;
                    try {
                        Packet.WritePacket(packet, output);
                    }
                    catch (Exception e) {
                        throw new PacketWriteException($"Failed to write packet of type '{packet.GetType().Name}'", e) {
                            WritesAttempted = count,
                            Written = sent
                        };
                    }
                }

                return sent;
            }
        }

        /// <summary>
        /// Queues a packet to be sent (adds it to <see cref="SendQueue"/>)
        /// </summary>
        /// <param name="packet"></param>
        public void SendPacket(Packet packet) {
            lock (this.sendQueue) {
                this.sendQueue.Enqueue(packet);
            }
        }

        /// <summary>
        /// Immidiately sends a packet to the connection in this packet system.
        /// This simply calls <see cref="Packet.WritePacket(Packet, IDataOutput)"/>
        /// <para>
        /// This method is blocking; you won't be able to do anything until ALL of the bytes have been written
        /// </para>
        /// </summary>
        /// <param name="packet">The packet to send (non-null)</param>
        public void SendPacketImmidiately(Packet packet) {
            Packet.WritePacket(packet, this.connection.Stream.Output);
        }
    }
}