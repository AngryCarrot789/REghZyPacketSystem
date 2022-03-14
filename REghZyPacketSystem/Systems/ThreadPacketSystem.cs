using System;
using System.Threading;
using REghZyPacketSystem.Exceptions;

#pragma warning disable 1591

namespace REghZyPacketSystem.Systems {
    /// <summary>
    /// An extension to the <see cref="PacketSystem"/>, using a read and write thread to enqueue packets
    /// that have been read from the connection, and to also sending packets to the connection
    /// <para>
    /// This prevents the main/important thread from having to wait for packets to be written and read; now it
    /// just has to process the packets (see below)
    /// </para>
    /// <para>
    /// This does not poll/process any packets, that must be done manually (because packet handlers/listeners
    /// aren't thread safe), via the <see cref="PacketSystem.ProcessReadQueue(int)"/> method
    /// </para>
    /// </summary>
    public class ThreadPacketSystem : PacketSystem, IDisposable {
        private static int READ_THREAD_COUNT = 0;
        private static int SEND_THREAD_COUNT = 0;

        // it should be "writeThread"... but i just cant... look... they're both the same number of chars :')
        protected readonly Thread readThread;
        protected readonly Thread sendThread;

        protected volatile bool shouldRun;
        protected volatile bool canRead;
        protected volatile bool canSend;

        protected volatile int writeCount;
        protected bool pauseThreads;

        private int readCount;
        private int sendCount;
        private volatile int threadSleepTime;
        private volatile bool disposed;

        private readonly object locker = new object();

        /// <summary>
        /// Whether this has been disposed or not
        /// </summary>
        public bool Disposed {
            get => this.disposed;
            private set => this.disposed = value;
        }

        public int ThreadSleepTime {
            get => this.threadSleepTime;
            set {
                if (value < 0) {
                    throw new ArgumentException("Value must be above or equal to 0", nameof(value));
                }

                this.threadSleepTime = value;
            }
        }

        /// <summary>
        /// The number of packets that the write thread should try to send each time
        /// <para>
        /// See the comments on <see cref="PacketSystem.ProcessSendQueue(int)"/>, this may not be the exact
        /// number of packets that get written every time. The ability to write more than 1 is only for extra speed... maybe
        /// </para>
        /// </summary>
        public int WriteCount {
            get => this.writeCount;
            set => this.writeCount = value;
        }

        /// <summary>
        /// Sets whether the read thread can run or not. If set to <see langword="false"/>, it will not stop
        /// the thread, it will simply sit at idle until this becomes <see langword="true"/>
        /// </summary>
        public bool CanRead {
            get => this.canRead;
            set => this.canRead = value;
        }

        /// <summary>
        /// Sets whether the send/write thread can run or not. If set to <see langword="false"/>, it will not stop
        /// the thread, it will simply sit at idle until this becomes <see langword="true"/>
        /// </summary>
        public bool CanSend {
            get => this.canSend;
            set => this.canSend = value;
        }

        /// <summary>
        /// The exact number of packets that have been read
        /// </summary>
        public int PacketsRead => this.readCount;

        /// <summary>
        /// The exact number of packets that have been sent
        /// </summary>
        public int PacketsSent => this.sendCount;

        /// <summary>
        /// The thread used to read packets
        /// </summary>
        public Thread ReadThread => this.readThread;

        /// <summary>
        /// The thread used to send packets
        /// </summary>
        public Thread SendThread => this.sendThread;

        /// <summary>
        /// Sets whether reading and writing packets is paused or not.
        /// <para>
        /// <see langword="false"/> means nothing can be read or written.
        /// <see langword="true"/> means packets can be read and written
        /// </para>
        /// </summary>
        public bool Paused {
            get => this.pauseThreads;
            set {
                if (value) {
                    this.canRead = false;
                    this.canSend = false;
                    this.pauseThreads = true;
                }
                else {
                    this.canRead = true;
                    this.canSend = true;
                    this.pauseThreads = false;
                }
            }
        }

        public delegate void PacketReadFail(ThreadPacketSystem system, PacketCreationException e);
        public delegate void PacketWriteFail(ThreadPacketSystem system, PacketWriteException e);
        public delegate void ReadAvailable(ThreadPacketSystem system);

        /// <summary>
        /// Called when an exception was thrown while reading a packet from the connection
        /// <para>
        /// This will be invoked from the read thread, therefore you must ensure your code is thread safe!
        /// </para>
        /// </summary>
        public event PacketReadFail OnPacketReadError;

        /// <summary>
        /// Called when an exception was thrown while writing a packet to the connection
        /// <para>
        /// This will be invoked from the write thread, therefore you must ensure your code is thread safe!
        /// </para>
        /// </summary>
        public event PacketWriteFail OnPacketWriteError;

        /// <summary>
        /// Called when there are packets available to be read in <see cref="PacketSystem.ReadQueue"/>
        /// <para>
        /// This event will be called from another thread, therefore you must ensure your code is thread safe!
        /// </para>
        /// </summary>
        public event ReadAvailable OnReadAvailable;

        /// <summary>
        /// Creates a new instance of the threaded packet system
        /// </summary>
        public ThreadPacketSystem(BaseConnection connection, int writeCount = 3) : base(connection) {
            this.threadSleepTime = 1;
            this.readThread = new Thread(ReadMain) {
                Name = $"REghZy Read Thread {++READ_THREAD_COUNT}"
            };

            this.sendThread = new Thread(WriteMain) {
                Name = $"REghZy Write Thread {++SEND_THREAD_COUNT}"
            };

            this.writeCount = writeCount;
            this.Paused = true;
        }

        /// <summary>
        /// Starts the base packet system, and both the read and write threads
        /// </summary>
        public override void Start() {
            base.Start();
            if (this.shouldRun) {
                this.Paused = false;
            }
            else {
                StartThreads();
            }
        }

        /// <summary>
        /// Disconnects from the connection, and stops the read and write threads
        /// </summary>
        public override void Stop() {
            this.Paused = true;
            lock (this.locker) {
                base.Stop();
            }
        }

        public void StartThreads() {
            if (this.shouldRun) {
                throw new Exception("Cannot re-start threads after they've been killed");
            }

            this.shouldRun = true;
            this.canRead = true;
            this.canSend = true;
            this.readThread.Start();
            this.sendThread.Start();
        }

        public void KillThreads() {
            if (!this.shouldRun) {
                throw new Exception("Threads have not been started yet");
            }

            this.shouldRun = false;
            this.canRead = false;
            this.canSend = false;
            this.readThread.Join();
            this.sendThread.Join();
            this.Disposed = true;
        }

        /// <summary>
        /// Sets <see cref="Paused"/> to true, stopping the threads from reading and writing
        /// </summary>
        public void PauseThreads() {
            this.Paused = true;
        }

        /// <summary>
        /// Sets <see cref="Paused"/> to false, allowing the threads to read and write again
        /// </summary>
        public void UnpauseThreads() {
            this.Paused = false;
        }

        private void ReadMain() {
            while (this.shouldRun) {
                while (this.canRead) {
                    bool locked = false;
                    bool read;
                    object lck = this.locker;
                    try {
                        Monitor.Enter(lck, ref locked);
                        // absolute last resort to ensure a read can happen
                        if (!this.connection.IsConnected || !this.canRead) {
                            break;
                        }

                        read = ReadNextPacket();
                    }
                    catch (PacketCreationException e) {
                        #if DEBUG
                        throw e;
                        #else
                        this.OnPacketReadError?.Invoke(this, e);
                        continue;
                        #endif
                    }
                    finally {
                        if (locked) {
                            Monitor.Exit(lck);
                        }
                    }

                    if (read) {
                        this.readCount++;
                        this.OnReadAvailable?.Invoke(this);
                    }
                    else {
                        DoThreadDelay();
                    }
                }

                // A big wait time, because it's very unlikely that the ability to read packets
                // will be changed in a very tight time window
                Thread.Sleep(50);
            }
        }

        private void WriteMain() {
            while (this.shouldRun) {
                while (this.canSend) {
                    bool locked = false;
                    int write;
                    object lck = this.locker;
                    try {
                        Monitor.Enter(lck, ref locked);
                        // absolute last resort to ensure a write can happen
                        if (!this.connection.IsConnected || !this.canSend) {
                            break;
                        }

                        write = ProcessSendQueue(this.writeCount);
                    }
                    catch (PacketWriteException e) {
                        #if DEBUG
                        throw e;
                        #else
                        this.OnPacketWriteError?.Invoke(this, e);
                        continue;
                        #endif
                    }
                    finally {
                        if (locked) {
                            Monitor.Exit(lck);
                        }
                    }

                    if (write == 0) {
                        DoThreadDelay();
                    }
                    else {
                        this.sendCount += write;
                    }
                }

                // A big wait time, because it's very unlikely that the ability to
                // write packets will be changed in a very tight time window... that
                // could change though... maybe... not really
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Disconnects and kills the threads used with this Threaded packet system
        /// </summary>
        public void Dispose() {
            KillThreads();
            Stop();
        }

        /// <summary>
        /// Used by the read and write threads to delay them, so that they aren't running at 100% all the time, consuming resources
        /// </summary>
        protected virtual void DoThreadDelay() {
            // this will actually delay for about 10-16ms~ average, due to thread time slicing stuff
            Thread.Sleep(this.threadSleepTime);
        }
    }
}
