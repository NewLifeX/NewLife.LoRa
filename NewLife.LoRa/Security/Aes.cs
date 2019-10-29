using System;

namespace NewLife.LoRa.Security
{
    class aes_context
    {
        public Byte[] ksch = new Byte[(14 + 1) * (4 * 4)];
        public Byte rnd;

        public void SetKey(Byte[] key)
        {

        }

        public Byte[] Encrypt(Byte[] data, Int32 length)
        {
            return data;
        }

        public Byte[] Decrypt(Byte[] data, Int32 length)
        {
            return data;
        }
    }

    class Aes
    {
    }
}
