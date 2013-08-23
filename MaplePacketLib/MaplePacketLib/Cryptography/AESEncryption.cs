﻿using System;
using System.Security.Cryptography;

namespace MaplePacketLib.Cryptography
{
    internal static class AESEncryption
    {
        private static byte[] sUserKey;
        private static ICryptoTransform sTransformer;

        public static bool KeySet
        {
            get
            {
                return sUserKey != null;
            }
        }

        internal static void SetKey(byte[] key)
        {
            sUserKey = new byte[32];
            System.Buffer.BlockCopy(key, 0, sUserKey, 0, 32);

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
