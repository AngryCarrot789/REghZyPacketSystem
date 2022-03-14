using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Systems.Handling {
    public interface IListener {
        /// <summary>
        /// Called when the given packet is received
        /// </summary>
        /// <param name="packet">The packet (not null)</param>
        void OnReceived(Packet packet);
    }
}
