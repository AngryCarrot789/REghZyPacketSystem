using System;
using REghZy.Streams;

namespace REghZyPacketSystem {
    /// <summary>
    /// Represents a connection to a data stream, with an ability to connect and disconnect
    /// </summary>
    public abstract class BaseConnection : IDisposable {
        /// <summary>
        /// Whether this instance is being disposed or not
        /// </summary>
        protected bool isDisposed = false;

        /// <summary>
        /// The data stream this connection has open
        /// <para>
        /// This may be null if <see cref="IsConnected"/> returns false
        /// </para>
        /// </summary>
        public abstract DataStream Stream { get; }

        /// <summary>
        /// Indicates whether this connection is open or not.
        /// This also indicates whether the input/output streams are available (they may be null if this is false)
        /// <para>
        /// Calling <see cref="Connect"/> should result in this being <see langword="true"/>
        /// </para>
        /// <para>
        /// Calling <see cref="Disconnect"/> should result in this being <see langword="false"/>
        /// </para>
        /// </summary>
        public abstract bool IsConnected { get; }

        protected BaseConnection() {

        }

        /// <summary>
        /// Creates the connection, allowing data to be read and written
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Breaks the connection, stopping data from being read and written
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// Disconnects and then connects
        /// </summary>
        public virtual void Restart() {
            Disconnect();
            Connect();
        }

        /// <summary>
        /// Disposes this connection, releasing all resources that it uses
        /// <para>
        /// This also means it cannot be re-connected to
        /// </para>
        /// </summary>
        public virtual void Dispose() {
            this.isDisposed = true;
        }
    }
}