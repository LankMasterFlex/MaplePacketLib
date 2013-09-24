using System;
using System.Net.Sockets;
using MaplePacketLib.Cryptography;

namespace MaplePacketLib
{
    /// <summary>
    /// Class that handlers connection with a Maplestory server
    /// </summary>
    public sealed class CClientSocket : IDisposable
    {
        /// <summary>
        /// Receive block size
        /// </summary>
        public const short ReceiveSize = 1024;

        private readonly IMapleClient m_recipient;

        private Socket m_socket;

        private MapleCipher m_clientCipher;
        private MapleCipher m_serverCipher;

        private bool m_disposed;
        private bool m_connected;

        private byte[] m_recvBuffer;
        private byte[] m_packetBuffer;
        private int m_cursor;

        private object m_sendLock;

        /// <summary>
        /// Gets the event recipient assigned at the constructor
        /// </summary>
        public IMapleClient MapleClient
        {
            get
            {
                return m_recipient;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the client is connected to a remote host as of the last operation
        /// </summary>
        public bool Connected
        {
            get
            {
                return m_connected;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the client is disposed
        /// </summary>
        public bool Disposed
        {
            get
            {
                return m_disposed;
            }
        }

        /// <summary>
        /// Sets the Aes key for encryption
        /// </summary>
        /// <param name="key">32 byte Aes key</param>
        public static void SetAesKey(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (key.Length != 32)
                throw new ArgumentOutOfRangeException("Key length needs to be 32", "key");

            for (int i = 0; i < 32; i++)
            {
                if (i % 4 != 0 && key[i] != 0)
                    throw new ArgumentException("Invalid Aes Key format", "key");
            }

            AESEncryption.SetKey(key);
        }

        /// <summary>
        ///  Creates a new instance of CClientSocket
        /// </summary>
        /// <param name="eventRecipient">The event recipient</param>
        public CClientSocket(IMapleClient eventRecipient)
        {
            if (!AESEncryption.KeySet)
            {
                throw new InvalidOperationException("Aes key is not set. Please set it with SetDefaultAesKey");
            }

            if (eventRecipient == null)
            {
                throw new ArgumentNullException("eventRecipient");
            }

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
                throw new InvalidOperationException("Socket is already connected");
            }

            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.NoDelay = true;
            m_socket.BeginConnect(ip, port, EndConnect, state);
        }

        private void EndConnect(IAsyncResult iar)
        {
            if (m_disposed) { return; }

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
                    Receive();
                }
            }
        }

        private void Receive()
        {
            if (!m_connected || m_disposed) { return; }

            SocketError error = SocketError.Success;

            m_socket.BeginReceive(m_recvBuffer, 0, ReceiveSize, SocketFlags.None, out error, PacketCallback, null);

            if (error != SocketError.Success)
            {
                Disconnect();
            }
        }

        private void Append(int length)
        {
            if (m_packetBuffer.Length - m_cursor < length)
            {
                int newSize = m_packetBuffer.Length * 2;

                while (newSize < m_cursor + length)
                    newSize *= 2;

                Array.Resize<byte>(ref m_packetBuffer, newSize);
            }

            Buffer.BlockCopy(m_recvBuffer, 0, m_packetBuffer, m_cursor, length);

            m_cursor += length;
        }

        private void PacketCallback(IAsyncResult iar)
        {
            if (!m_connected || m_disposed) { return; }

            SocketError error = SocketError.Success;

            int length = m_socket.EndReceive(iar, out error);

            if (length == 0 || error != SocketError.Success)
            {
                Disconnect();
            }
            else
            {
                Append(length);
                ManipulateBuffer();
                Receive();
            }
        }

        private void ManipulateBuffer()
        {
            if (m_clientCipher == null || m_serverCipher == null)
            {
                if (m_cursor >= 2)
                {
                    short packetSize = BitConverter.ToInt16(m_packetBuffer, 0);

                    if (m_cursor >= packetSize + 2)
                    {
                        byte[] buffer = new byte[packetSize];
                        Buffer.BlockCopy(m_packetBuffer, 2, buffer, 0, packetSize);

                        PacketReader packet = new PacketReader(buffer);
                        
                        short major = packet.ReadShort();
                        string minor = packet.ReadMapleString();

                        m_clientCipher = new MapleCipher(major, packet.ReadBytes(4), MapleCipher.CipherMode.Encrypt);
                        m_serverCipher = new MapleCipher(major, packet.ReadBytes(4), MapleCipher.CipherMode.Decrypt);
                        
                        byte locale = packet.ReadByte();

                        m_recipient.OnHandshake(major, minor, locale);

                        m_cursor = 0;
                    }
                }
            }
            else
            {
                while (m_cursor > 4) //header room
                {
                    int packetSize = MapleCipher.GetPacketLength(m_packetBuffer);

                    if (m_cursor < packetSize + 4) //header + packet room
                    {
                        break;
                    }

                    byte[] packetBuffer = new byte[packetSize];
                    Buffer.BlockCopy(m_packetBuffer, 4, packetBuffer, 0, packetSize); //copy packet
                    m_serverCipher.Transform(packetBuffer); //decrypt

                    m_cursor -= packetSize + 4; //fix len

                    if (m_cursor > 0) //move reamining bytes
                    {
                        Buffer.BlockCopy(m_packetBuffer, packetSize + 4, m_packetBuffer, 0, m_cursor);
                    }

                    m_recipient.OnPacket(packetBuffer);
                }
            }
        }

        /// <summary>
        /// Encrypts and sends packet to server
        /// </summary>
        /// <param name="packet">Packet to send</param>
        public void Send(PacketWriter packet)
        {
            ThrowIfDisposed();

            if (!m_connected)
            {
                throw new InvalidOperationException("Socket is not connected");
            }

            if (m_clientCipher == null || m_serverCipher == null)
            {
                throw new InvalidOperationException("Handshake has not been received yet");
            }

            byte[] data = packet.ToArray();

            if (data.Length < 2)
            {
                throw new ArgumentOutOfRangeException("Packet length must be greater than 2", "packet");
            }

            lock (m_sendLock)
            {
                byte[] final = new byte[data.Length + 4];

                m_clientCipher.GetHeaderToServer(data.Length, final);
                m_clientCipher.Transform(data);

                Buffer.BlockCopy(data, 0, final, 4, data.Length);

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

            packet.Close();
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
                m_socket.Disconnect(false);
                m_socket.Dispose();

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
            if (!m_disposed)
            {
                Disconnect();

                m_disposed = true;

                m_packetBuffer = null;
                m_recvBuffer = null;
                m_cursor = 0;

                m_sendLock = null;
            }
        }
    }
}
