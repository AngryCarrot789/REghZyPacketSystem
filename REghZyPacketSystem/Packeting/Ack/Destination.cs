namespace REghZyPacketSystem.Packeting.Ack {
    /// <summary>
    /// The ACK packet direction
    /// </summary>
    public enum Destination : byte {
        /// <summary>
        /// This packet is going to the server
        /// </summary>
        ToServer = 0b0001,

        /// <summary>
        /// This packet is being acknowledged by the server
        /// </summary>
        Ack = 0b0100,

        /// <summary>
        /// This packet is going to the client
        /// </summary>
        ToClient = 0b0010
    }
}