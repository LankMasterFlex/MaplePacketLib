using System;
using System.Security.Cryptography;

namespace MaplePacketLib.Cryptography
{
    internal static class AESEncryption
    {
        private static ICryptoTransform sTransformer;

        public static bool KeySet
        {
            get
            {
                return sTransformer != default(ICryptoTransform);
            }
        }

        internal static void SetKey(byte[] key)
        {
            RijndaelManaged aes = new RijndaelManaged()
            {
                Key = key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            using (aes)
            {
                sTransformer = aes.CreateEncryptor();
            }
        }

        public static void Transform(byte[] data, int size, byte[] IV)
        {
            byte[] morphKey = new byte[16];
            int remaining = size;
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
