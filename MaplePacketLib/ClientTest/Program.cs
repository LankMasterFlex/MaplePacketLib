using System;
using System.Threading;
using MaplePacketLib;

namespace ClientTest
{
    internal sealed class Client : IMapleClient
    {
        private readonly CClientSocket m_socket;

        public Client()
        {
            m_socket = new CClientSocket(this);
        }

        public void ConnectToLogin()
        {
            m_socket.Connect("8.31.99.141", 8484);
        }

        public void OnConnect(bool success, object state)
        {
            Console.WriteLine(success ? "Connected to server" : "Unable to connect to server");
        }

        public void OnHandshake(short major, string minor, byte locale)
        {
            Console.WriteLine("Maplestory Handshake v{0}.{1}", major, minor);

        }

        public void OnPacket(byte[] packet)
        {
            Console.WriteLine("Dataload: {0}", BitConverter.ToString(packet));

            PacketReader reader = new PacketReader(packet);
            short opcode = reader.ReadShort();

            switch (opcode)
            {
                case 0x11: //ping
                    m_socket.Send(new PacketWriter(0x46, 2)); //pong
                    break;
            }
        }

        public void OnDisconnected()
        {
            Console.WriteLine("Disconnected from server");
        }
    }
    internal static class Program
    {
        private static readonly byte[] sUserKey = new byte[32] //141.1
        {
           0x5C, 0x00, 0x00, 0x00,
           0xC0, 0x00, 0x00, 0x00,
           0xC0, 0x00, 0x00, 0x00,
           0x86, 0x00, 0x00, 0x00,
           0xEA, 0x00, 0x00, 0x00,
           0x85, 0x00, 0x00, 0x00,
           0x03, 0x00, 0x00, 0x00,
           0x37, 0x00, 0x00, 0x00
        };

        static void Main(string[] args)
        {
            CClientSocket.SetAesKey(sUserKey);

            Client c = new Client();
            c.ConnectToLogin();

            while (true)
                Thread.Yield();
        }
    }
}
