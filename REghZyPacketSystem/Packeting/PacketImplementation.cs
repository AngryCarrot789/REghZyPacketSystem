using System;
using System.Linq.Expressions;
using System.Reflection;

namespace REghZyPacketSystem.Packeting {
    /// <summary>
    /// A implementation of a packet. This is used to locate unloaded packet classes during app startup
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class PacketImplementation : Attribute {
        public ushort PacketID { get; }

        /// <summary>
        /// If true (default value is true), then the packet will be registered automatically when <see cref="Packet.Setup()"/> is called
        /// <para>
        /// If this is false, then the packet's static constructor will be executed,
        /// allowing the packet to be registered in there instead of the auto-registration
        /// </para>
        /// </summary>
        public bool UseAutoRegister { get; }

        public PacketImplementation(ushort packetId) : this(packetId, true) {

        }

        public PacketImplementation(ushort packetId, bool useAutoRegister) {
            this.PacketID = packetId;
            this.UseAutoRegister = useAutoRegister;
        }

        /// <summary>
        /// Attempts to register the packet
        /// </summary>
        /// <returns>True if the packet was registered, otherwise false if it was already registered</returns>
        internal bool TryRegister(Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type), "The packet type cannot be null");
            }

            if (!typeof(Packet).IsAssignableFrom(type)) {
                throw new ArgumentException($"The type {type.Name} does not extend Packet", nameof(type));
            }

            if (!this.UseAutoRegister) {
                throw new Exception("UseAutoRegister is set to false; cannot attempt to register");
            }

            if (Packet.IsRegistered(type)) {
                return false;
            }

            Packet.Register(type, this.PacketID, MakeCreator(type));
            return true;
        }

        internal static Func<Packet> MakeCreator(Type type) {
            if (!typeof(Packet).IsAssignableFrom(type)) {
                throw new ArgumentException("The type " + type.Name + " does not extend the Packet class");
            }

            ConstructorInfo ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, Type.EmptyTypes, null);
            if (ctor == null) {
                throw new ArgumentException("Missing empty constructor for packet type: " + type.Name, nameof(type));
            }

            return Expression.Lambda<Func<Packet>>(Expression.New(ctor)).Compile();
        }
    }
}