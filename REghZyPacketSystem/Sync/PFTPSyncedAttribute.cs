using System;
using System.Reflection;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Sync {
    /// <summary>
    /// A packet-field to target-property synced attribute. This is the most common type of sync attribute
    /// </summary>
    public sealed class PFTPSyncedAttribute : SyncedAttribute {
        private readonly FieldInfo packetInfo;
        private PropertyInfo targetInfo;

        public PFTPSyncedAttribute(Type packetType, string packetDataName) : base(packetType, packetDataName) {
            this.packetInfo = packetType.GetField(packetDataName);
            if (this.packetInfo == null) {
                if (packetType.GetProperty(packetDataName) != null) {
                    throw new Exception($"A PP2TF sync attribute was used, when a PP2TP should've been used, for {packetType.Name}->{packetDataName}");
                }
                else {
                    throw new Exception($"Unknown member name {packetType.Name}->{packetDataName}");
                }
            }
        }

        public override void Update(Packet packet, object target) {
            this.targetInfo.SetValue(target, this.packetInfo.GetValue(packet));
        }

        public override void LoadDetails(Type targetType, string targetDataName) {
            this.targetInfo = targetType.GetProperty(targetDataName);
            this.TargetType = targetType;
            this.TargetDataName = targetDataName;
            if (this.targetInfo == null) {
                if (targetType.GetField(targetDataName) != null) {
                    throw new Exception($"A PF2TP sync attribute was used, when a PP2TF should've been used, for {targetType.Name}->{targetDataName}");
                }
                else {
                    throw new Exception($"Unknown member name {targetType.Name}->{targetDataName}");
                }
            }
        }
    }
}