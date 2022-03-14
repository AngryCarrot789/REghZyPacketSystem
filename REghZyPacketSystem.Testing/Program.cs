using REghZyPacketSystem.Serial;
using REghZyPacketSystem.Systems;
using System;
using System.Threading;
using REghZyPacketSystem.Packeting;

namespace REghZyPacketSystem.Testing {
    class Program {
        public ThreadPacketSystem systemA;
        public ThreadPacketSystem systemB;

        public AckProcessor2Counter counterA;
        public AckProcessor2Counter counterB;

        public static void Main(string[] args) {
            // i don't like not using the 'this' keyword :(
            new Program();
        }

        public Program() {
            Packet.Setup();
            this.systemA = new ThreadPacketSystem(new SerialConnection("COM20"));
            this.systemB = new ThreadPacketSystem(new SerialConnection("COM21"));
            this.counterB = new AckProcessor2Counter(this.systemB);

            this.systemA.RegisterListener<Packet1Chat>((p) => {
                Console.WriteLine($"[B] -> [A] \"{p.msg}\"");
                this.systemA.SendPacket(new Packet1Chat() {msg = "AAAAAAAA!!!"});
            });

            this.systemB.RegisterListener<Packet1Chat>((p) => {
                Console.WriteLine($"[A] -> [B] \"{p.msg}\"");
                this.systemB.SendPacket(new Packet1Chat() {msg = "BBBBBBBBBBBB!!!"});
            });

            this.systemA.Start();
            this.systemB.Start();

            this.systemA.OnReadAvailable += system => system.ProcessReadQueue();
            this.systemB.OnReadAvailable += system => system.ProcessReadQueue();

            Thread.Sleep(5);
            this.systemA.SendPacket(new Packet1Chat() { msg = "ello there lol" });
            Thread.Sleep(1000);
            this.systemA.Dispose();
            this.systemB.Dispose();
        }
    }
}
