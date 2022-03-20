namespace REghZyPacketSystem.Systems.Handling {
    /// <summary>
    /// Packet receiving priority
    /// </summary>
    public enum Priority {
        /// <summary>
        /// This packet must be received first
        /// </summary>
        HIGHEST = 0,

        /// <summary>
        /// This packet must be received very soon after coming in
        /// </summary>
        HIGH = 1,

        /// <summary>
        /// Just normal; this is typically used for monitoring
        /// </summary>
        NORMAL = 2,
        
        /// <summary>
        /// Quite low
        /// </summary>
        LOW = 3,

        /// <summary>
        /// Same as low, but even lower
        /// </summary>
        LOWEST = 4
    }
}
