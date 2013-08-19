using System;
using System.Net.Sockets;
using MaplePacketLib.Cryptography;

namespace MaplePacketLib
{
    public sealed class CClientSocket : IDisposable
    {
        public const short ReceiveSize = 1024;

        private readonly Socket m_socket;
        private readonly IMapleClient m_recipient;

        private MapleCipher m_clientCipher;
        private MapleCipher m_serverCipher;

        private bool m_disposed;
        private bool m_connected;

        private byte[] m_recvBuffer;
        private byte[] m_packetBuffer;
        private int m_cursor;

        private object m_sendLock;

        public bool Connected
        {
            get
            {
                return m_connected;
            }
        }

        /// <summary>
        ///  Creates a new instance of CClientSocket
        /// </summary>
        /// <param name="eventRecipient">The event recipient</param>
        public CClientSocket(IMapleClient eventRecipient)
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_recipient = eventRecipient;

            m_disposed = false;
            m_connected = false;

            m_packetBuffer = new byte[ReceiveSize];
            m_recvBuffer = new byte[ReceiveSize];
            m_cursor = 0;

            m_sendLock = new object();
        }

        /// <summary>
        /// Connects client to a endpoint
        /// </summary>
        /// <param name="ip">Host IP</param>
        /// <param name="port">Host Port</param>
        /// <param name="state">User object</param>
        public void Connect(string ip, short port, object state = null)
        {
            ThrowIfDisposed();

            if (m_connected)
            {
                throw new Exception("Socket is already connected");
            }

            m_socket.BeginConnect(ip, port, EndConnect, state);
        }

        private void EndConnect(IAsyncResult iar)
        {
            try
            {
                m_socket.EndConnect(iar);
                m_connected = true;
            }
            catch (SocketException)
            {
                m_connected = false;
            }
            finally
            {
                m_recipient.OnConnect(m_connected, iar.AsyncState);

                if (m_connected)
                {
                    var buffer = new byte[16];
                    Receive(buffer, 0, 16, HandshakeCallback, buffer);
                }
            }
        }

        private void Receive(byte[] buffer, int offset, int length, AsyncCallback callback, object state = null)
        {
            if (!m_connected || m_disposed) { return; }

            SocketError error = SocketError.Success;

            m_socket.BeginReceive(buffer, offset, length, SocketFlags.None, out error, callback, state);

            if (error != SocketError.Success)
            {
                Disconnect();
            }
        }

        private void Append(byte[] data, int start, int length)
        {
            if (m_packetBuffer.Length - m_cursor < length)
            {
                int newSize = m_packetBuffer.Length * 2;
                
                while (newSize < m_cursor + length)
                    newSize *= 2;

                Array.Resize<byte>(ref m_packetBuffer, newSize);
            }

            System.Buffer.BlockCopy(data, start, m_packetBuffer, m_cursor, length);

            m_cursor += length;
        }

        private void HandshakeCallback(IAsyncResult iar)
        {
            if (!m_connected || m_disposed) { return; }

            byte[] buffer = iar.AsyncState as byte[];

            SocketError error = SocketError.Success;

            int length = m_socket.EndReceive(iar, out error);

            if (length < 16 || error != SocketError.Success)
            {
                Disconnect();
                return;
            }

            PacketReader packet = new PacketReader(buffer);
            packet.ReadShort(); //length header
            short major = packet.ReadShort();
            string minor = packet.ReadMapleString();
            m_clientCipher = new MapleCipher(major, packet.ReadBytes(4), MapleCipher.TransformDirection.Encrypt);
            m_serverCipher = new MapleCipher(major, packet.ReadBytes(4), MapleCipher.TransformDirection.Decrypt);
            byte locale = packet.ReadByte();

            m_recipient.OnHandshake(major, minor);

            Receive(m_recvBuffer, 0, ReceiveSize, PacketCallback);
        }
        private void PacketCallback(IAsyncResult iar)
        {
            if (!m_connected || m_disposed) { return; }

            SocketError error = SocketError.Success;

            int length = m_socket.EndReceive(iar, out error);

            if (length == 0 || error != SocketError.Success)
            {
                Disconnect();
                return;
            }

            Append(m_recvBuffer, 0, length);

            while (true)
            {
                if (m_cursor < 4) //header room
                {
                    break;
                }

                int packetSize = MapleCipher.GetPacketLength(m_packetBuffer);

                if (m_cursor < packetSize + 4) //header + packet room
                {
                    break;
                }

                byte[] packetBuffer = new byte[packetSize];
                System.Buffer.BlockCopy(m_packetBuffer, 4, packetBuffer, 0, packetSize); //copy packet
                m_serverCipher.Transform(packetBuffer); //decrypt

                m_cursor -= packetSize + 4; //fix len

                if (m_cursor > 0) //move reamining bytes
                {
                    System.Buffer.BlockCopy(m_packetBuffer, packetSize + 4, m_packetBuffer, 0, m_cursor);
                }

                m_recipient.OnPacket(packetBuffer);
            }

            Receive(m_recvBuffer, 0, ReceiveSize, PacketCallback);
        }

        /// <summary>
        /// Sends packet to server
        /// </summary>
        /// <param name="p">Packet to send</param>
        public void Send(PacketWriter p)
        {
            ThrowIfDisposed();

            if (!m_connected)
            {
                throw new Exception("Socket is not connected");
            }

            if (m_clientCipher == null)
            {
                throw new Exception("Client cipher not yet set");
            }

            byte[] packet = p.ToArray();

            if (packet == null)
            {
                throw new Exception("Packet is null");
            }

            if (packet.Length < 2)
            {
                throw new Exception("Packet length must be greater than 2");
            }

            lock (m_sendLock)
            {

                byte[] final = new byte[packet.Length + 4];

                m_clientCipher.GetHeaderToServer(packet.Length, final);
                m_clientCipher.Transform(packet);

                System.Buffer.BlockCopy(packet, 0, final, 4, packet.Length);

                int offset = 0;

                while (offset < final.Length)
                {
                    SocketError errorCode = SocketError.Success;
                    int sent = m_socket.Send(final, offset, final.Length - offset, SocketFlags.None, out errorCode);

                    if (sent == 0 || errorCode != SocketError.Success)
                    {
                        Disconnect();
                        return;
                    }

                    offset += sent;
                }
            }

            p.Close();
        }

        /// <summary>
        /// Disconnects from host
        /// </summary>
        public void Disconnect()
        {
            ThrowIfDisposed();

            if (m_connected)
            {
                m_connected = false;
                m_socket.Shutdown(SocketShutdown.Both);
                m_socket.Disconnect(true);

                m_clientCipher = null;
                m_serverCipher = null;

                m_recipient.OnDisconnected();
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        /// <summary>
        /// Disconnects and disposes the client
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposed();

            m_disposed = true;

            Disconnect();

            m_socket.Dispose();

            m_packetBuffer = null;
            m_recvBuffer = null;
            m_cursor = 0;

            m_sendLock = null;
        }
    }
}
