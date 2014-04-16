using MaplePacketLib.Cryptography;
using System;
using System.Net;
using System.Net.Sockets;

namespace MaplePacketLib
{
    public sealed class Acceptor : IDisposable
    {
        public const int Backlog = 10;

        private readonly ServerInfo m_info;
        private readonly AesCipher m_aes;
        private readonly int m_port;
        private readonly TcpListener m_listener;

        private bool m_started;
        private bool m_disposed;

        public int Port
        {
            get
            {
                return m_port;
            }
        }

        public event EventHandler<Session> OnClientAccepted;

        public Acceptor(ServerInfo info,AesCipher aes,int port)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            if (aes == null)
                throw new ArgumentNullException("aes");

            m_info = info;
            m_aes = aes;
            m_port = port;

            m_started = false;
            m_disposed = false;

            m_listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            if (m_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (m_started)
                throw new InvalidOperationException("Already listening");

            m_listener.Start(Backlog);
            m_listener.BeginAcceptSocket(EndAccept, null);
        }

        private void EndAccept(IAsyncResult iar)
        {
            if (!m_disposed)
            {
                var socket = m_listener.EndAcceptSocket(iar);
                var session = new Session(socket, SessionType.Server, m_aes);

                if (OnClientAccepted != null)
                    OnClientAccepted(this, session);

                session.Start(m_info);

                if (!m_disposed)
                    m_listener.BeginAcceptSocket(EndAccept, null);
            }
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                m_disposed = true;
                m_listener.Server.Close();
            }
        }
    }
}
