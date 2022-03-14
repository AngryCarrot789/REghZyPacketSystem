using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using REghZy.Streams;
using REghZyPacketSystem.Exceptions;
using REghZyPacketSystem.Sync;
using REghZyPacketSystem.Systems;

namespace REghZyPacketSystem.Packeting {
    /// <summary>
    /// A base class for all packets
    /// </summary>
    public abstract class Packet {
        private static bool Initialised = false;

        /// <summary>
        /// Whether to use the preamble when sending and receiving packets
        /// <para>
        /// The preamble allows at least 2 bytes to be lost while reading the very start of the
        /// packet header, but results in an extra 4 bytes of data being read/written
        /// </para>
        /// </summary>
        public static bool UsePreamble = true;

        // Packet data structure
        //
        // [ Protocol ] [ ID ] [ Length ] [ Payload ]

        private static readonly Dictionary<ushort, Type> IdToType;
        private static readonly Dictionary<Type, ushort> TypeToId;
        private static readonly Dictionary<ushort, Func<Packet>> IdToCreator;

        // protocol headers
        private static readonly byte[] INIT_PREAMBLE_ARRAY = new byte[32];
        private const byte INIT_PREAMBLE = 0b01010101; // 85, just because it's 1010 etc lol
        private const byte PREAMBLE_SEQ1 = 0b01110010; // 114 - 'r' - stands for REghZy 2.1 (as in the version of the packet structure :-))
        private const byte PREAMBLE_SEQ2 = 0b01111010; // 122 - 'z'
        private const byte PREAMBLE_SEQ3 = 0b00110010; // 50  - '2'
        private const byte PREAMBLE_SEQ4 = 0b00110010; // 50  - '2'

        /// <summary>
        /// The absolute minimum size of a packet's header. As of REghZyPacketSystem-2.2, this is 4 bytes
        /// <para>
        /// If <see cref="UsePreamble"/> is set to true, then, as of v2.2, this will be 8 bytes
        /// </para>
        /// </summary>
        public static ushort MinimumHeaderSize => (ushort) ((UsePreamble ? 4 : 0) + 2 + 2); // preamble? + id + payload size

        /// <summary>
        /// The maximum size of a packet, including the packet header
        /// </summary>
        public static ushort MaximumPayloadSize => (ushort) (short.MaxValue - MinimumHeaderSize);

        /// <summary>
        /// Finds all packet classes in all assemblies attributed with <see cref="PacketImplementation"/> and tries to register them
        /// </summary>
        public static void Setup() {
            if (Initialised) {
                return;
            }

            Initialised = true;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (Type type in assembly.GetTypes().Where(t => typeof(Packet).IsAssignableFrom(t))) {
                    PacketImplementation implementation = type.GetCustomAttribute<PacketImplementation>(false);
                    if (implementation != null) {
                        if (implementation.UseAutoRegister) {
                            implementation.TryRegister(type);
                        }
                        else {
                            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                        }
                    }
                }
            }
        }

        static Packet() {
            IdToType = new Dictionary<ushort, Type>();
            TypeToId = new Dictionary<Type, ushort>();
            IdToCreator = new Dictionary<ushort, Func<Packet>>();
            for (int i = 0; i < INIT_PREAMBLE_ARRAY.Length; i++) {
                INIT_PREAMBLE_ARRAY[i] = INIT_PREAMBLE;
            }
        }

        protected Packet() {

        }

        /// <summary>
        /// Tries to automatically register a packet based on it's packet implementation annotation
        /// </summary>
        /// <typeparam name="T">The packet type</typeparam>
        public static void AutoRegister<T>() where T : Packet {
            AutoRegister(typeof(T));
        }

        /// <summary>
        /// Tries to automatically register a packet based on it's packet implementation annotation
        /// </summary>
        /// <typeparam name="T">The packet type</typeparam>
        public static void AutoRegister(Type type) {
            if (!typeof(Packet).IsAssignableFrom(type)) {
                throw new ArgumentException("The type is not a packet type", nameof(type));
            }

            PacketImplementation implementation = type.GetCustomAttribute<PacketImplementation>(false);
            if (implementation != null) {
                if (implementation.UseAutoRegister) {
                    implementation.TryRegister(type);
                }
                else {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
            }
        }

        /// <summary>
        /// Registers the given packet ID and the creator. The creator is only used to reduce the use of reflection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="creator"></param>
        public static void Register<T>(ushort id, Func<T> creator) where T : Packet {
            if (IdToCreator.ContainsKey(id)) {
                throw new Exception($"Packet with id '{id}' has already been registered");
            }

            if (!typeof(Packet).IsAssignableFrom(typeof(T))) {
                throw new ArgumentException($"The type {typeof(T).Name} does not extend Packet", nameof(T));
            }

            if (creator == null) {
                creator = Activator.CreateInstance<T>;
            }

            IdToCreator[id] = creator;
            IdToType[id] = typeof(T);
            TypeToId[typeof(T)] = id;
        }

        /// <summary>
        /// Registers the given packet ID and the creator. The creator is only used to reduce the use of reflection
        /// </summary>
        public static void Register(Type type, ushort id, Func<Packet> creator) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type), "Packet type cannot be null");
            }

            if (!typeof(Packet).IsAssignableFrom(type)) {
                throw new ArgumentException($"The type {type.Name} does not extend Packet", nameof(type));
            }

            if (IsRegistered(type)) {
                throw new Exception($"{type.Name} with id '{id}' has already been registered");
            }

            if (creator == null) {
                creator = () => (Packet) Activator.CreateInstance(type);
            }

            IdToCreator[id] = creator;
            IdToType[id] = type;
            TypeToId[type] = id;
        }

        internal static bool IsRegistered(Type type) {
            return TypeToId.ContainsKey(type);
        }

        /// <summary>
        /// The number of bytes in the packet's payload
        /// </summary>
        public abstract ushort GetPayloadSize();

        /// <summary>
        /// Reads all of the packet's payload from the given input.
        /// The payload length is also specified, because it's sent in the packet header
        /// </summary>
        public abstract void ReadPayLoad(IDataInput input, ushort length);

        /// <summary>
        /// Writes all of the packet's payload into the given data output
        /// </summary>
        /// <param name="output"></param>
        public abstract void WritePayload(IDataOutput output);

        /// <summary>
        /// Writes the given packet to the given data output
        /// <para>
        /// Writes the protocol header, then packet header, and then the packet's payload
        /// </para>
        /// </summary>
        /// <param name="packet">The packet to write</param>
        /// <param name="output">The data output to write to</param>
        public static void WritePacket(Packet packet, IDataOutput output) {
            if (UsePreamble) {
                WriteProtocolHeader(output);
            }

            WritePacketHeader(packet, output);
            packet.WritePayload(output);
            output.Flush();
        }

        /// <summary>
        /// Reads a packet from the given data stream
        /// <para>
        /// Reads the protocol header, then packet header, and then the packet's payload
        /// </para>
        /// </summary>
        /// <param name="input">The data input to read from</param>
        /// <returns>
        /// A packet (non-null)
        /// </returns>
        public static Packet ReadPacket(IDataInput input) {
            if (UsePreamble) {
                try {
                    ReadProtoolHeader(input);
                }
                catch (DataLossException e) {
                    throw new PacketCreationException("Failed to read protocol header", e);
                }
            }

            ushort id = input.ReadUShort();
            if (IdToCreator.TryGetValue(id, out Func<Packet> creator)) {
                ushort length = input.ReadUShort();
                if (length >= MaximumPayloadSize) {
                    throw new PacketCreationException($"Payload length ({length}) was larger than the max size ({MaximumPayloadSize})");
                }

                Packet packet = creator();
                try {
                    packet.ReadPayLoad(input, length);
                }
                catch (Exception e) {
                    throw new PacketPayloadException($"Failed to read payload from packet type '{packet.GetType().Name}'", e);
                }

                return packet;
            }
            else {
                throw new PacketCreationException($"Missing creator for packet ID: {id}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetPacketID<T>() {
            return TypeToId.TryGetValue(typeof(T), out ushort id) ? id : (ushort) 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetPacketID(Type type) {
            return TypeToId.TryGetValue(type, out ushort id) ? id : (ushort) 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort GetPacketID(Packet packet) {
            return TypeToId.TryGetValue(packet.GetType(), out ushort id) ? id : (ushort) 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetPacketType(ushort id) {
            return IdToType.TryGetValue(id, out Type type) ? type : null;
        }

        public static Packet CreateInstance(ushort id) {
            if (IdToCreator.TryGetValue(id, out Func<Packet> creator)) {
                return creator();
            }

            throw new Exception("Tried to create unregistered packet instance with id: " + id);
        }

        public static T CreateInstance<T>() where T : Packet {
            if (TypeToId.TryGetValue(typeof(T), out ushort id)) {
                return (T) CreateInstance(id);
            }

            throw new Exception("Tried to create unregistered packet instance with type: " + nameof(T));
        }

        public static Packet CreateInstance(Type type) {
            if (TypeToId.TryGetValue(type, out ushort id)) {
                return CreateInstance(id);
            }

            throw new Exception("Tried to create unregistered packet instance with type: " + type.Name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WritePacketHeader(Packet packet, IDataOutput output) {
            output.WriteUShort(TypeToId[packet.GetType()]);
            output.WriteUShort(packet.GetPayloadSize());
        }

        /// <summary>
        /// Tells the sync table to update all annotated fields/properties with this packet's data, for all targets listening to this type of packet
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAnnotated() {
            SyncTable.BroadcastUpdateInternal(this);
        }

        /// <summary>
        /// Tells the sync table to update all annotated fields/properties (for the given target) with this packet's values
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateAnnotated(object target) {
            SyncTable.UpdateFor(this, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteProtocolHeader(IDataOutput output) {
            output.WriteByte(PREAMBLE_SEQ1);
            output.WriteByte(PREAMBLE_SEQ2);
            output.WriteByte(PREAMBLE_SEQ3);
            output.WriteByte(PREAMBLE_SEQ4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadProtoolHeader(IDataInput input) {
            byte seq1 = input.ReadByte();
            if (seq1 == PREAMBLE_SEQ1) {
                if (input.ReadByte() == PREAMBLE_SEQ2) {
                    if (input.ReadByte() == PREAMBLE_SEQ3) {
                        if (input.ReadByte() == PREAMBLE_SEQ4) {
                            return;
                        }
                        else {
                            throw new DataLossException("Incorrect protocol header received; TTTF");
                        }
                    }
                    else {
                        throw new DataLossException("Incorrect protocol header received; TTF?");
                    }
                }
                else {
                    throw new DataLossException("Incorrect protocol header received; TF??");
                }
            }
            else if (seq1 == PREAMBLE_SEQ2) {
                if (input.ReadByte() == PREAMBLE_SEQ3) {
                    if (input.ReadByte() == PREAMBLE_SEQ4) {
                        return;
                    }
                    else {
                        throw new DataLossException("Incorrect protocol header received; FTTF");
                    }
                }
                else {
                    throw new DataLossException("Incorrect protocol header received; FTF?");
                }
            }
            else if (seq1 == PREAMBLE_SEQ3) {
                if (input.ReadByte() == PREAMBLE_SEQ4) {
                    return;
                }
                else {
                    throw new DataLossException("Incorrect protocol header received; FFTF");
                }
            }
            else {
                throw new DataLossException("Incorrect protocol header received; FFF? (missing 3/4 sequence bytes)");
            }
        }
    }
}
