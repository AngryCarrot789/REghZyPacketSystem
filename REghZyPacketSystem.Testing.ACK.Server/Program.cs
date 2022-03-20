using System;
using System.Threading;
using REghZyPacketSystem.Packeting;
using REghZyPacketSystem.Serial;
using REghZyPacketSystem.Systems;
using REghZyPacketSystem.Systems.Handling;

namespace REghZyPacketSystem.Testing.ACK.Server {
    class Program {
        private readonly ThreadPacketSystem system;
        private readonly AckProcessor2Counter counter;

        static void Main(string[] args) {
            Packet.Setup();
            new Program();
        }

        public Program() {
            Packet.AutoRegister<Packet1Chat>();
            Packet.AutoRegister<Packet2Counter>();
            BaseConnection connection = new SerialConnection("COM20");
            this.system = new ThreadPacketSystem(connection);
            this.counter = new AckProcessor2Counter(this.system);

            // don't care about processing on another thread
            this.system.OnReadAvailable += OnSystemOnOnReadAvailable;
            this.system.OnPacketReadError += (sys, e) => Console.WriteLine("Error reading: " + e);
            this.system.OnPacketWriteError += (sys, e) => Console.WriteLine("Error writing: " + e);
            this.system.RegisterListener((p) => {
                Console.WriteLine($"Received {p.GetType().Name} -> {p}");
            }, Priority.HIGHEST);

            this.system.StartThreads();
            this.system.Connection.Connect();

            while (true) {
                Thread.Sleep(100);
            }
        }

        private static void OnSystemOnOnReadAvailable(ThreadPacketSystem system) {
            system.ProcessReadQueue();
        }
    }
}