using System;
using System.Threading;
using REghZyPacketSystem.Exceptions;

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

        protected volatile int writePerTick;
        protected bool pauseThreads;

        private int readCount;
        private int sendCount;
        private volatile int intervalTime;
        private volatile int sleepTime;
        private volatile bool disposed;

        private readonly object readLock = new object();
        private readonly object writeLock = new object();

        /// <summary>
        /// Whether this has been disposed or not
        /// </summary>
        public bool Disposed {
            get => this.disposed;
            private set => this.disposed = value;
        }

        /// <summary>
        /// The amount of time to pause the read/write threads per tick while reading/writing is enabled
        /// </summary>
        public int ThreadIntervalDelayTime {
            get => this.intervalTime;
            set {
                if (value < 0) {
                    throw new ArgumentException("Value must be above or equal to 0", nameof(value));
                }

                this.intervalTime = value;
            }
        }

        /// <summary>
        /// The amount of time to pause the read/write threads while reading/writing is disabled. By default, this is 50 (milliseconds)
        /// </summary>
        public int ThreadSleepTime {
            get => this.sleepTime;
            set {
                if (value < 0) {
                    throw new ArgumentException("Value must be above or equal to 0", nameof(value));
                }

                this.sleepTime = value;
            }
        }

        /// <summary>
        /// The number of packets that the write thread should try to send each time
        /// <para>
        /// See the comments on <see cref="PacketSystem.ProcessSendQueue(int)"/>, this may not be the exact
        /// number of packets that get written every time. The ability to write more than 1 is only for extra speed... maybe
        /// </para>
        /// </summary>
        public int WritePerTick {
            get => this.writePerTick;
            set => this.writePerTick = value;
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
        public ThreadPacketSystem(BaseConnection connection, int writePerTick = 3) : base(connection) {
            this.intervalTime = 1;
            this.sleepTime = 50;
            this.readThread = new Thread(ReadMain) {
                Name = $"REghZy Read Thread {++READ_THREAD_COUNT}"
            };

            this.sendThread = new Thread(WriteMain) {
                Name = $"REghZy Write Thread {++SEND_THREAD_COUNT}"
            };

            this.writePerTick = writePerTick;
            this.Paused = true;
        }

        /// <summary>
        /// Starts the threads. This should only be invoked once, see <see cref="Paused"/> to pause/unpause the threads
        /// </summary>
        public virtual void StartThreads() {
            if (this.shouldRun) {
                throw new Exception("Cannot re-start threads after they've been killed");
            }

            this.shouldRun = true;
            this.canRead = true;
            this.canSend = true;
            this.readThread.Start();
            this.sendThread.Start();
        }

        /// <summary>
        /// Kills the threads making them un-restartable. This should only be invoked once, see <see cref="Paused"/> to pause/unpause threads
        /// <para>
        /// This effectively disposes the thread packet system
        /// </para>
        /// </summary>
        public virtual void KillThreads() {
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
        public void ResumeThreads() {
            this.Paused = false;
        }

        private void ReadMain() {
            while (this.shouldRun) {
                while (this.canRead) {
                    bool locked = false;
                    bool read;
                    object lck = this.readLock;
                    try {
                        Monitor.Enter(lck, ref locked);
                        // absolute last resort to ensure a read can happen
                        if (this.connection.IsConnected && this.canRead) {
                            read = ReadNextPacket();
                        }
                        else {
                            break;
                        }
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
                        DoTickDelay();
                    }
                }

                DoSleepDelay();
            }
        }

        private void WriteMain() {
            while (this.shouldRun) {
                while (this.canSend) {
                    bool locked = false;
                    int write;
                    object lck = this.writeLock;
                    try {
                        Monitor.Enter(lck, ref locked);
                        // absolute last resort to ensure a write can happen
                        if (this.connection.IsConnected && this.canSend) {
                            write = ProcessSendQueue(this.writePerTick);
                        }
                        else {
                            break;
                        }
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
                        DoTickDelay();
                    }
                    else {
                        this.sendCount += write;
                    }
                }

                DoSleepDelay();
            }
        }

        /// <summary>
        /// Disconnects and kills the threads used with this Threaded packet system
        /// </summary>
        public virtual void Dispose() {
            KillThreads();
        }

        /// <summary>
        /// Used by the read and write threads to delay them, so that they aren't running at 100% all the time, consuming resources
        /// </summary>
        protected virtual void DoTickDelay() {
            // this will actually delay for about 10-16ms~ average, due to thread time slicing stuff
            Thread.Sleep(this.intervalTime);
        }

        /// <summary>
        /// Used by the read and write threads to delay them while <see cref="CanSend"/>/<see cref="CanRead"/> is false
        /// <para>
        /// This will usually pause for quite a long time (10-50ms), but it can be removed
        /// </para>
        /// </summary>
        protected virtual void DoSleepDelay() {
            // A big wait time, because it's very unlikely that the ability to
            // read/write packets will be changed in a very tight time window... 
            Thread.Sleep(this.sleepTime);
        }
    }
}