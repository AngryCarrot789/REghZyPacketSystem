using System;
using System.Reflection;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Sync {
    /// <summary>
    /// A packet-property to target-field synced attribute
    /// </summary>
    public sealed class PPTFSyncedAttribute : SyncedAttribute {
        private readonly PropertyInfo packetInfo;
        private FieldInfo targetInfo;

        public PPTFSyncedAttribute(Type packetType, string packetDataName) : base(packetType, packetDataName) {
            this.packetInfo = packetType.GetProperty(packetDataName);
            if (this.packetInfo == null) {
                if (packetType.GetField(packetDataName) != null) {
                    throw new Exception($"A PP2TP sync attribute was used, when a PF2TP should've been used, for {packetType.Name}->{packetDataName}");
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
            this.targetInfo = targetType.GetField(targetDataName);
            this.TargetType = targetType;
            this.TargetDataName = targetDataName;
            if (this.targetInfo == null) {
                if (targetType.GetProperty(targetDataName) != null) {
                    throw new Exception($"A PP2TF sync attribute was used, when a PP2TP should've been used, for {targetType.Name}->{targetDataName}");
                }
                else {
                    throw new Exception($"Unknown member name {targetType.Name}->{targetDataName}");
                }
            }
        }
    }
}