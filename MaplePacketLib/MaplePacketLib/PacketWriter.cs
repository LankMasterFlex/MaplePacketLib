using System;
using System.IO;

namespace MaplePacketLib
{
    /// <summary>
    /// Class used to write a little endian byte stream
    /// </summary>
    public class PacketWriter : IDisposable
    {
        private MemoryStream m_stream;
        private bool m_disposed;

        /// <summary>
        /// Position of stream
        /// </summary>
        public int Position
        {
            get
            {
                return (int)m_stream.Position;
            }
            set
            {
                m_stream.Position = value;
            }
        }
        /// <summary>
        /// Creates a new instance of PacketReader with the default buffer size
        /// </summary>
        public PacketWriter()
        {
            m_stream = new MemoryStream(32);
            m_disposed = false;
        }

        /// <summary>
        /// Creates a new instance of PacketReader
        /// </summary>
        /// <param name="opcode">Value to be written as short</param>
        /// <param name="size">Buffer size</param>
        public PacketWriter(short opcode, int size = 64)
        {
            m_stream = new MemoryStream(size);
            m_disposed = false;
            WriteShort(opcode);
        }

        /// <summary>
        /// Converts and writes bytes to stream - From LittleEndianByteConverter by Shoftee
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="byteCount">Size of bytes in value</param>
        private void Append(long value, int byteCount)
        {
            for (int i = 0; i < byteCount; i++)
            {
                m_stream.WriteByte((byte)value);
                value >>= 8;
            }
        }

        /// <summary>
        /// Writes a byte to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteByte(int value = 0)
        {
            ThrowIfDisposed();
            m_stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a bool to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteBool(bool value = false)
        {
            ThrowIfDisposed();
            WriteByte(value ? 1 : 0);
        }

        /// <summary>
        /// Writes bytes to the stream
        /// </summary>
        /// <param name="values">Value to be written</param>
        public void WriteBytes(params byte[] values)
        {
            ThrowIfDisposed();
            m_stream.Write(values, 0, values.Length);
        }

        /// <summary>
        /// Writes a short to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteShort(short value = 0)
        {
            ThrowIfDisposed();
            Append(value, 2);
        }

        /// <summary>
        /// Writes a unsigned short to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteUShort(ushort value = 0)
        {
            ThrowIfDisposed();
            Append(value, 2);
        }

        /// <summary>
        /// Writes a int to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteInt(int value = 0)
        {
            ThrowIfDisposed();
            Append(value, 4);
        }

        /// <summary>
        /// Writes a unsiged int to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteUInt(uint value = 0)
        {
            ThrowIfDisposed();
            Append(value, 4);
        }

        /// <summary>
        /// Writes a long to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteLong(long value = 0)
        {
            ThrowIfDisposed();
            Append(value, 8);
        }

        /// <summary>
        /// Writes a unsigned long to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteULong(ulong value = 0)
        {
            ThrowIfDisposed();
            Append((long)value, 8);
        }

        /// <summary>
        /// Writes zero bytes to the stream
        /// </summary>
        /// <param name="count">Amount of zeros</param>
        public void WriteZero(int count)
        {
            ThrowIfDisposed();

            for (int i = 0; i < count; i++)
                WriteByte();
        }

        /// <summary>
        /// Writes a string to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteString(string value)
        {
            ThrowIfDisposed();

            foreach (char c in value)
                WriteByte((byte)c);
        }

        /// <summary>
        /// Writes a maple string to the stream
        /// </summary>
        /// <param name="value">Value to be written</param>
        public void WriteMapleString(string value = "")
        {
            ThrowIfDisposed();

            WriteShort((short)value.Length);
            WriteString(value);
        }

        /// <summary>
        /// Writes the value cased as T to the stream
        /// Using not recommended.
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="value">Value to write</param>
        public void Write<T>(object value)
        {
            ThrowIfDisposed();

            Type type = typeof(T);

            if (type == typeof(byte))
            {
                WriteByte((byte)value);
            }
            else if (type == typeof(short))
            {
                WriteShort((short)value);
            }
            else if (type == typeof(ushort))
            {
                WriteUShort((ushort)value);
            }
            else if (type == typeof(int))
            {
                WriteInt((int)value);
            }
            else if (type == typeof(uint))
            {
                WriteUInt((uint)value);
            }
            else if (type == typeof(long))
            {
                WriteLong((long)value);
            }
            else if (type == typeof(ulong))
            {
                WriteULong((ulong)value);
            }
            else
            {
                throw new Exception("TYPE NOT SUPPORTED");
            }
        }

        /// <summary>
        /// Retrieves the stream
        /// </summary>
        /// <returns>Packet</returns>
        public byte[] ToArray()
        {
            ThrowIfDisposed();
            return m_stream.ToArray();
        }

        /// <summary>
        /// Closes the stream
        /// </summary>
        public void Close()
        {
            if (!m_disposed)
            {
                m_disposed = true;

                m_stream.Dispose();
                m_stream = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        /// Disposes the stream
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
