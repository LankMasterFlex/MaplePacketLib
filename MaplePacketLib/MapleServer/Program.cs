using MaplePacketLib;
using MaplePacketLib.Cryptography;
using System;

namespace MapleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var userkey = new byte[] //europe maplestory key
            {
                0x13, 0x00, 0x00, 0x00,
                0x08, 0x00, 0x00, 0x00,
                0x06, 0x00, 0x00, 0x00,
                0xB4, 0x00, 0x00, 0x00,
                0x1B, 0x00, 0x00, 0x00,
                0x0F, 0x00, 0x00, 0x00,
                0x33, 0x00, 0x00, 0x00,
                0x52, 0x00, 0x00, 0x00
            };

            var aes = new AesCipher(userkey);

            var info = new ServerInfo()
            {
                Version = 97,
                Subversion = "1",
                Locale = 9
            };

            using (var acceptor = new Acceptor(info, aes, 8484))
            {
                acceptor.OnClientAccepted += OnClientAccepted;
                acceptor.Start();

                Console.ReadLine();
            }
        }

        static void OnClientAccepted(object sender, Session e)
        {
            Console.WriteLine("Accepted a client!");

            e.OnPacket += OnPacket;
            e.OnDisconnected += OnDisconnected;
        }

        static void OnPacket(object sender, byte[] e)
        {
            Console.WriteLine("[Recv] {0}", BitConverter.ToString(e));
        }

        static void OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Client disconnected");
        }
    }
}
