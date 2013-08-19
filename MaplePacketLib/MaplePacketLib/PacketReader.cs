namespace MaplePacketLib
{
    /// <summary>
    /// Class used to read a little endian byte stream
    /// </summary>
    public class PacketReader
    {
        private readonly byte[] m_buffer;
        private int m_offset;

        /// <summary>
        /// Position of stream
        /// </summary>
        public int Position
        {
            get
            {
                return m_offset;
            }
            set
            {
                m_offset = value;
            }
        }

        /// <summary>
        /// Bytes left to read in the stream
        /// </summary>
        public int Avaiable
        {
            get
            {
                return m_buffer.Length - m_offset;
            }
        }

        /// <summary>
        /// Creates a new instance of PacketReader
        /// </summary>
        /// <param name="buffer">Stream to read from</param>
        public PacketReader(params byte[] buffer)
        {
            m_buffer = buffer;
            m_offset = 0;
        }

        /// <summary>
        /// Reads bytes from stream - From LittleEndianByteConverter by Shoftee
        /// </summary>
        /// <param name="count">Bytes to read</param>
        /// <returns>Read value</returns>
        private long FromBytes(int count)
        {
            long result = 0;
            int position = (m_offset + count) - 1;

            for (; position >= m_offset; position--)
            {
                result = (result << 8) | m_buffer[position];
            }

            m_offset += count;

            return result;
        }

        /// <summary>
        /// Reads a byte array
        /// </summary>
        /// <param name="length">Amount of bytes to be read</param>
        /// <returns>A byte array</returns>
        public byte[] ReadBytes(int length)
        {
            var result = new byte[length];
            System.Buffer.BlockCopy(m_buffer, m_offset, result, 0, length);
            m_offset += length;
            return result;
        }

        /// <summary>
        /// Reads a byte from the stream
        /// </summary>
        /// <returns>A byte</returns>
        public byte ReadByte()
        {
            return m_buffer[m_offset++];
        }

        /// <summary>
        /// Reads a bool from the stream
        /// </summary>
        /// <returns>A bool</returns>
        public bool ReadBool()
        {
            return m_buffer[m_offset++] != 0;
        }

        /// <summary>
        /// Reads a short from the stream
        /// </summary>
        /// <returns>A short</returns>
        public short ReadShort()
        {
            return (short)FromBytes(2);
        }

        /// <summary>
        /// Reads a unsigned short from the stream
        /// </summary>
        /// <returns>A unsigned short</returns>
        public ushort ReadUShort()
        {
            return (ushort)FromBytes(2);
        }

        /// <summary>
        /// Reads a int from the stream
        /// </summary>
        /// <returns>A int</returns>
        public int ReadInt()
        {
            return (int)FromBytes(4);
        }

        /// <summary>
        /// Reads a unsigned int from the stream
        /// </summary>
        /// <returns>A unsigned int</returns>
        public uint ReadUInt()
        {
            return (uint)FromBytes(4);
        }

        /// <summary>
        /// Reads a long from the stream
        /// </summary>
        /// <returns>A long</returns>
        public long ReadLong()
        {
            return FromBytes(8);
        }

        /// <summary>
        /// Reads a unsigned long from the stream
        /// </summary>
        /// <returns>A unsigned long</returns>
        public ulong ReadULong()
        {
            return (ulong)FromBytes(8);
        }

        /// <summary>
        /// Increases streams position
        /// </summary>
        /// <param name="count">Bytes to skip</param>
        public void Skip(int count)
        {
            m_offset += count;
        }

        /// <summary>
        /// Reads a string from the stream
        /// </summary>
        /// <param name="count">Characters in string</param>
        /// <returns>A string</returns>
        public string ReadString(int count)
        {
            char[] final = new char[count];

            for (int i = 0; i < count; i++)
            {
                final[i] = (char)ReadByte();
            }

            return new string(final);
        }

        /// <summary>
        /// Reads a maple string from the stream
        /// </summary>
        /// <returns>A string</returns>
        public string ReadMapleString()
        {
            short count = ReadShort();
            return ReadString(count);
        }

        /// <summary>
        /// Retrieves the inital buffer 
        /// </summary>
        /// <returns>Initial buffer</returns>
        public byte[] ToArray()
        {
            return m_buffer;
        }
    }
}
