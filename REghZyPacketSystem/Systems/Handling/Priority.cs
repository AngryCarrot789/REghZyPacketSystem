namespace REghZyPacketSystem.Systems.Handling {
    public enum Priority {
        /// <summary>
        /// This packet must be received first ALL OF THE TIME
        /// </summary>
        HIGHEST = 0,

        /// <summary>
        /// This packet must be received very soon after coming in
        /// </summary>
        HIGH = 1,

        /// <summary>
        /// Doesn't really matter. This is typically used for monitoring
        /// </summary>
        NORMAL = 2,
        
        /// <summary>
        /// Really doesn't matter at all
        /// </summary>
        LOW = 3,

        /// <summary>
        /// Same as low, but even lower
        /// </summary>
        LOWEST = 4
    }
}
