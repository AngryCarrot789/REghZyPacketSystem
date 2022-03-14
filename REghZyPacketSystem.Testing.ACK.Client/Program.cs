using System;
using System.Threading;
using REghZyPacketSystem.Packeting;
using REghZyPacketSystem.Serial;
using REghZyPacketSystem.Systems;
using REghZyPacketSystem.Systems.Handling;

namespace REghZyPacketSystem.Testing.ACK.Client {
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
            this.system = new ThreadPacketSystem(new SerialConnection("COM21"));
            this.counter = new AckProcessor2Counter(this.system);

            this.system.OnReadAvailable += system => system.ProcessReadQueue();
            this.system.OnPacketReadError += (sys, e) => Console.WriteLine("Error reading: " + e);
            this.system.OnPacketWriteError += (sys, e) => Console.WriteLine("Error writing: " + e);
            this.system.RegisterListener((p) => {
                Console.WriteLine($"Received {p.GetType().Name} -> {p}");
            }, Priority.HIGHEST);

            // this.system.SendPacketImmidiately(new Packet1Chat() {msg = "elloooo"});

            this.system.Start();

            PrintResponce(this.counter.MakeRequestAsync(new Packet2Counter() {action = Packet2Counter.CountAction.incr}).Result);
            PrintResponce(this.counter.MakeRequestAsync(new Packet2Counter() {action = Packet2Counter.CountAction.incr}).Result);
            PrintResponce(this.counter.MakeRequestAsync(new Packet2Counter() {action = Packet2Counter.CountAction.decr}).Result);

            Console.WriteLine("Done!!!");

            Thread.Sleep(1000);
            this.system.Dispose();
        }

        public static void PrintResponce(Packet2Counter counter) {
            Console.WriteLine($"Packet2Counter -> {counter.action} -> {counter.count}");
        }
    }
}