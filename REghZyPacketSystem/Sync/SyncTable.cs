using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Sync {
    /// <summary>
    /// A thread-safe helper for creating synchronisation between packets and objects
    /// </summary>
    public static class SyncTable {
        private static readonly HashSet<Type> BAKED_TYPES;
        private static readonly Dictionary<Type, HashSet<object>> PACKET_TO_TARGET; // packet -> targets
        private static readonly Dictionary<Type, HashSet<SyncedAttribute>> TARGET_TO_ATTRIB; // target -> target attributes

        private static readonly object LOCK = new object();

        static SyncTable() {
            BAKED_TYPES = new HashSet<Type>();
            TARGET_TO_ATTRIB = new Dictionary<Type, HashSet<SyncedAttribute>>(128);
            PACKET_TO_TARGET = new Dictionary<Type, HashSet<object>>(128);
        }

        private static HashSet<SyncedAttribute> GetAttributes(Type targetType) {
            HashSet<SyncedAttribute> attributes;
            if (!TARGET_TO_ATTRIB.TryGetValue(targetType, out attributes)) {
                TARGET_TO_ATTRIB[targetType] = attributes = new HashSet<SyncedAttribute>();
            }

            return attributes;
        }

        private static HashSet<object> GetTargets(Type packetType) {
            HashSet<object> handlers;
            if (!PACKET_TO_TARGET.TryGetValue(packetType, out handlers)) {
                PACKET_TO_TARGET[packetType] = handlers = new HashSet<object>();
            }

            return handlers;
        }

        /// <summary>
        /// Registers the given target, allowing it to receive updates from the given packet type
        /// </summary>
        public static void Register(object target) {
            if (target == null) {
                throw new ArgumentNullException("target", "Target cannot be null");
            }

            lock (LOCK) {
                bool requireBake = !BAKED_TYPES.Contains(target.GetType());
                Type targetType = target.GetType();
                HashSet<SyncedAttribute> attributes = requireBake ? GetAttributes(targetType) : null;
                if (requireBake) {
                    BAKED_TYPES.Add(targetType);
                }

                foreach (PropertyInfo info in targetType.GetProperties()) {
                    foreach (SyncedAttribute attribute in info.GetCustomAttributes<SyncedAttribute>()) {
                        if (requireBake) {
                            attribute.LoadDetails(targetType, info.Name);
                            attributes.Add(attribute);
                        }

                        GetTargets(attribute.PacketType).Add(target);
                    }
                }

                foreach (FieldInfo info in targetType.GetFields()) {
                    foreach (SyncedAttribute attribute in info.GetCustomAttributes<SyncedAttribute>()) {
                        if (requireBake) {
                            attribute.LoadDetails(targetType, info.Name);
                            attributes.Add(attribute);
                        }

                        GetTargets(attribute.PacketType).Add(target);
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters the given target, meaning it will no longer receive updates from packets
        /// </summary>
        public static void Unregister(object target) {
            if (target == null) {
                return;
            }

            // does not un-bake the type, just in case it needs to be used again
            lock (LOCK) {
                if (TARGET_TO_ATTRIB.TryGetValue(target.GetType(), out HashSet<SyncedAttribute> attributes)) {
                    foreach (SyncedAttribute attribute in attributes) {
                        if (PACKET_TO_TARGET.TryGetValue(attribute.PacketType, out HashSet<object> handlers)) {
                            if (handlers.Remove(target) && handlers.Count == 0) {
                                PACKET_TO_TARGET.Remove(attribute.PacketType);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates all objects that are listening to the type of the given packet, and only updates the specific target data
        /// </summary>
        /// <param name="packet">The packet that targets will receive data from</param>
        /// <exception cref="ArgumentNullException">The packet is null</exception>
        public static void BroadcastUpdate(Packet packet) {
            if (packet == null) {
                throw new ArgumentNullException("packet", "Packet cannot be null");
            }

            BroadcastUpdateInternal(packet);
        }

        internal static void BroadcastUpdateInternal(Packet packet) {
            Type packetType = packet.GetType();
            lock (LOCK) {
                if (PACKET_TO_TARGET.TryGetValue(packetType, out HashSet<object> handlers)) {
                    foreach (object handler in handlers) {
                        UpdateForInternal(packet, packetType, handler, handler.GetType());
                    }
                }
            }
        }

        /// <summary>
        /// Updates all objects that are listening to the type of the given packet, and updates all of the target data
        /// </summary>
        /// <param name="packet">The packet that targets will receive data from</param>
        /// <exception cref="ArgumentNullException">The packet is null</exception>
        public static void UpdateFor(Packet packet, object target) {
            if (packet == null) {
                throw new ArgumentNullException(nameof(packet), "Packet cannot be null");
            }

            if (target == null) {
                throw new ArgumentNullException(nameof(target), "Target cannot be null");
            }

            if (!BAKED_TYPES.Contains(target.GetType())) {
                throw new ArgumentException("Target type was not baked and therefore isn't registered");
            }

            UpdateForInternal(packet, packet.GetType(), target, target.GetType());
        }

        internal static void UpdateForInternal(Packet packet, Type packetType, object target, Type targetType) {
            if (TARGET_TO_ATTRIB.TryGetValue(targetType, out HashSet<SyncedAttribute> attributes)) {
                foreach (SyncedAttribute attribute in attributes.Where(attribute => attribute.PacketType.IsAssignableFrom(packetType))) {
                    attribute.Update(packet, target);
                }
            }
        }
    }
}