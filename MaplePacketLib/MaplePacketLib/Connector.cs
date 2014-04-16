using MaplePacketLib.Cryptography;
using System;
using System.Net.Sockets;

namespace MaplePacketLib
{
    public sealed class Connector
    {
        private readonly string m_ip;
        private readonly int m_port;
        private readonly AesCipher m_aes;

        public event EventHandler<Session> OnConnected;
        public event EventHandler<SocketError> OnError;

        public Connector(string ip,int port,AesCipher aes)
        {
            m_ip = ip;
            m_port = port;
            m_aes = aes;
        }

        public void Connect()
        {
            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.BeginConnect(m_ip, m_port, EndConnect, sock);
        }

        private void EndConnect(IAsyncResult iar)
        {
            var sock = iar.AsyncState as Socket;

            try
            {
                sock.EndConnect(iar);

                var session = new Session(sock,SessionType.Client,m_aes);

                if(OnConnected != null)
                    OnConnected(this,session);

                session.Start(null);
            }
            catch(SocketException se)
            {
                if (OnError != null)
                    OnError(this, se.SocketErrorCode);
            }

        }
    }
}
