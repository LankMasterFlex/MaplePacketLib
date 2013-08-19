using System;
using System.Security.Cryptography;

namespace MaplePacketLib.Cryptography
{
    static class AESEncryption
    {
        public const short AesKeyVersion = 140;

        private static readonly byte[] sUserKey = new byte[32] //140.1
        {
            0xCD, 0x00, 0x00, 0x00, 0x5C, 0x00, 0x00, 0x00, 0xDC, 0x00, 0x00, 0x00,0x98, 0x00, 0x00, 0x00,
            0xD8, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x9A, 0x00, 0x00, 0x00, 0x47, 0x00, 0x00, 0x00
        };

        private static ICryptoTransform sTransformer;

        static AESEncryption()
        {
            RijndaelManaged aes = new RijndaelManaged()
            {
                Key = sUserKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            using (aes)
            {
                sTransformer = aes.CreateEncryptor();
            }
        }

        public static void Transform(byte[] data, byte[] IV)
        {
            byte[] morphKey = new byte[16];
            int remaining = data.Length;
            int start = 0;
            int length = 0x5B0;

            while (remaining > 0)
            {
                for (int i = 0; i < 16; i++)
                    morphKey[i] = IV[i % 4];

                if (remaining < length)
                    length = remaining;

                for (int index = start; index < (start + length); index++)
                {
                    if ((index - start) % 16 == 0)
                        sTransformer.TransformBlock(morphKey, 0, 16, morphKey, 0);

                    data[index] ^= morphKey[(index - start) % 16];
                }

                start += length;
                remaining -= length;
                length = 0x5B4;
            }
        }
    }
}
