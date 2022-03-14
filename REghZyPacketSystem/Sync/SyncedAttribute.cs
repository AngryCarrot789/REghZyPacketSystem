using System;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Sync {
    /// <summary>
    /// An attribute that is to be used on fields or properties, allowing them to be synchronised with packet data
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class SyncedAttribute : Attribute {
        /// <summary>
        /// The type of packet that this synchronisation will be paying attention to
        /// </summary>
        public Type PacketType { get; }

        /// <summary>
        /// The name of the field or property that the packet type contains
        /// </summary>
        public string PacketDataName { get; }

        /// <summary>
        /// The target object (aka object that will be updated when a packet of type <see cref="PacketType"/> is received)
        /// </summary>
        public Type TargetType { get; internal set; }

        /// <summary>
        /// The name of the field or property that the target contains that will be updated
        /// with <see cref="PacketDataName"/> from the packet of type <see cref="PacketType"/>
        /// </summary>
        public string TargetDataName { get; internal set; }

        protected SyncedAttribute(Type packetType, string packetDataName) {
            this.PacketType = packetType;
            this.PacketDataName = packetDataName;
        }

        /// <summary>
        /// Updates the target with the data of the given packet. If the target doesn't use data from the given packet, then it will be ignored
        /// <para>
        /// It's very important that this method does not invoke any function
        /// in the <see cref="SyncTable"/>, otherwise a possible deadlock could occur
        /// </para>
        /// </summary>
        public abstract void Update(Packet packet, object target);

        /// <summary>
        /// Loads the target data into this attribute
        /// </summary>
        public abstract void LoadDetails(Type targetType, string targetDataName);
    }
}