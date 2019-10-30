using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.LoRa.Security
{
    class LoRaMacCrypto
    {
        #region 属性
        ///// <summary>CMAC/AES Message Integrity Code (MIC) Block B0 size</summary>
        //const int LORAMAC_MIC_BLOCK_B0_SIZE = 16;

        ///// <summary>MIC 初始化数据</summary>
        //private byte[] MicBlockB0 = { 0x49, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        ///// <summary>MIC 计算，共16字节，仅使用前4字节</summary>
        //private byte[] Mic;

        ///// <summary>Encryption aBlock and sBlock</summary>
        //private byte[] aBlock = { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        //private byte[] sBlock = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        ///// <summary>AES computation context</summary>
        //private aes_context AesContext;

        ///// <summary>CMAC computation context</summary>
        //private AES_CMAC_CTX AesCmacCtx;
        #endregion

        /// <summary>计算 LoRaMAC 的 MIC</summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="key">AES密钥</param>
        /// <param name="address">帧地址</param>
        /// <param name="dir">方向，0上行，1下行</param>
        /// <param name="sequenceCounter">帧序号计数器</param>
        public UInt32 LoRaMacComputeMic(Byte[] buffer, Byte[] key, uint address, Boolean dir, uint sequenceCounter)
        {
            var size = buffer.Length;

            var block = new Byte[] { 0x49, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            block[5] = (byte)(dir ? 1 : 0);

            block[6] = (byte)((address) & 0xFF);
            block[7] = (byte)((address >> 8) & 0xFF);
            block[8] = (byte)((address >> 16) & 0xFF);
            block[9] = (byte)((address >> 24) & 0xFF);

            block[10] = (byte)((sequenceCounter) & 0xFF);
            block[11] = (byte)((sequenceCounter >> 8) & 0xFF);
            block[12] = (byte)((sequenceCounter >> 16) & 0xFF);
            block[13] = (byte)((sequenceCounter >> 24) & 0xFF);

            block[15] = (byte)(size & 0xFF);

            var ctx = new AES_CMAC_CTX();
            ctx.Init();
            ctx.SetKey(key);
            ctx.Update(block);
            ctx.Update(buffer);

            return ctx.Final();

            //AES_CMAC_Init(AesCmacCtx);

            //AES_CMAC_SetKey(AesCmacCtx, key);

            //AES_CMAC_Update(AesCmacCtx, MicBlockB0, LORAMAC_MIC_BLOCK_B0_SIZE);

            //AES_CMAC_Update(AesCmacCtx, buffer, size & 0xFF);

            //AES_CMAC_Final(Mic, AesCmacCtx);

            //*mic = (uint)((uint)Mic[3] << 24 | (uint)Mic[2] << 16 | (uint)Mic[1] << 8 | (uint)Mic[0]);
        }

        /// <summary>加密</summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="key">AES密钥</param>
        /// <param name="address">帧地址</param>
        /// <param name="dir">方向，0上行，1下行</param>
        /// <param name="sequenceCounter">帧序号计数器</param>
        public Byte[] LoRaMacPayloadEncrypt(Byte[] buffer, Byte[] key, uint address, Boolean dir, uint sequenceCounter)
        {
            var size = buffer.Length;

            ushort i;
            byte bufferIndex = 0;
            ushort ctr = 1;

            //memset1(AesContext.ksch, '\0', 240);
            //aes_set_key(key, 16, &AesContext);

            var ctx = new aes_context();
            ctx.SetKey(key);

            var aBlock = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            //var sBlock = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            aBlock[5] = (byte)(dir ? 1 : 0);

            aBlock[6] = (Byte)((address) & 0xFF);
            aBlock[7] = (Byte)((address >> 8) & 0xFF);
            aBlock[8] = (Byte)((address >> 16) & 0xFF);
            aBlock[9] = (Byte)((address >> 24) & 0xFF);

            aBlock[10] = (Byte)((sequenceCounter) & 0xFF);
            aBlock[11] = (Byte)((sequenceCounter >> 8) & 0xFF);
            aBlock[12] = (Byte)((sequenceCounter >> 16) & 0xFF);
            aBlock[13] = (Byte)((sequenceCounter >> 24) & 0xFF);

            Byte[] encBuffer = new byte[size];
            while (size >= 16)
            {
                aBlock[15] = (Byte)((ctr) & 0xFF);
                ctr++;
                //aes_encrypt(aBlock, sBlock, &AesContext);
                var sBlock = ctx.Encrypt(aBlock, 16);
                for (i = 0; i < 16; i++)
                {
                    encBuffer[bufferIndex + i] = (Byte)(buffer[bufferIndex + i] ^ sBlock[i]);
                }
                size -= 16;
                bufferIndex += 16;
            }

            if (size > 0)
            {
                aBlock[15] = (Byte)((ctr) & 0xFF);
                //aes_encrypt(aBlock, sBlock, &AesContext);
                var sBlock = ctx.Encrypt(aBlock, 16);
                for (i = 0; i < size; i++)
                {
                    encBuffer[bufferIndex + i] = (Byte)(buffer[bufferIndex + i] ^ sBlock[i]);
                }
            }

            return encBuffer;
        }

        /// <summary>解密</summary>
        /// <param name="buffer">数据缓冲区</param>
        /// <param name="key">AES密钥</param>
        /// <param name="address">帧地址</param>
        /// <param name="dir">方向，0上行，1下行</param>
        /// <param name="sequenceCounter">帧序号计数器</param>
        public Byte[] LoRaMacPayloadDecrypt(Byte[] buffer, Byte[] key, uint address, Boolean dir, uint sequenceCounter)
        {
            return LoRaMacPayloadEncrypt(buffer, key, address, dir, sequenceCounter);
        }

        public uint LoRaMacJoinComputeMic(Byte[] buffer, Byte[] key)
        {
            var ctx = new AES_CMAC_CTX();
            ctx.Init();
            ctx.SetKey(key);
            ctx.Update(buffer);

            return ctx.Final();

            //AES_CMAC_Init(AesCmacCtx);

            //AES_CMAC_SetKey(AesCmacCtx, key);

            //AES_CMAC_Update(AesCmacCtx, buffer, size & 0xFF);

            //AES_CMAC_Final(Mic, AesCmacCtx);

            //*mic = (uint)((uint)Mic[3] << 24 | (uint)Mic[2] << 16 | (uint)Mic[1] << 8 | (uint)Mic[0]);
        }

        public Byte[] LoRaMacJoinDecrypt(Byte[] buffer, Byte[] key)
        {
            var size = buffer.Length;

            var aes = new aes_context();
            aes.SetKey(key);
            var decBuffer = aes.Encrypt(buffer, 16);

            //memset1(AesContext.ksch, '\0', 240);
            //aes_set_key(key, 16, &AesContext);
            //aes_encrypt(buffer, decBuffer, &AesContext);
            // Check if optional CFList is included
            if (size >= 16)
            {
                //aes_encrypt(buffer + 16, decBuffer + 16, &AesContext);
                var decBuffer2 = aes.Encrypt(buffer.ReadBytes(16), 16);
                decBuffer = decBuffer.Combine(decBuffer2);
            }

            return decBuffer;
        }

        public Byte[] LoRaMacJoinEncrypt(Byte[] buffer, Byte[] key)
        {
            var size = buffer.Length;

            var aes = new aes_context();
            aes.SetKey(key);
            var decBuffer = aes.Decrypt(buffer, 16);

            //memset1(AesContext.ksch, '\0', 240);
            //aes_set_key(key, 16, &AesContext);
            //aes_decrypt(buffer, encBuffer, &AesContext);
            // Check if optional CFList is included
            if (size >= 16)
            {
                //aes_decrypt(buffer + 16, encBuffer + 16, &AesContext);
                var decBuffer2 = aes.Decrypt(buffer.ReadBytes(16), 16);
                decBuffer = decBuffer.Combine(decBuffer2);
            }

            return decBuffer;
        }

        public void LoRaMacJoinComputeSKeys(Byte[] key, Byte[] appNonce, ushort devNonce, out byte[] nwkSKey, out byte[] appSKey)
        {
            var aes = new aes_context();
            aes.SetKey(key);

            //byte[] nonce = new byte[16];
            //byte[] pDevNonce = devNonce;

            //memset1(AesContext.ksch, '\0', 240);
            //aes_set_key(key, 16, &AesContext);

            //memset1(nonce, 0, sizeof(nonce));
            //nonce[0] = 0x01;
            //memcpy1(nonce + 1, appNonce, 6);
            //memcpy1(nonce + 7, pDevNonce, 2);
            //aes_encrypt(nonce, nwkSKey, &AesContext);

            byte[] nonce = new byte[16];
            nonce[0] = 0x01;
            nonce.Write(1, appNonce);
            nonce.Write(devNonce, 7);
            nwkSKey = aes.Encrypt(nonce, 16);

            //memset1(nonce, 0, sizeof(nonce));
            //nonce[0] = 0x02;
            //memcpy1(nonce + 1, appNonce, 6);
            //memcpy1(nonce + 7, pDevNonce, 2);
            //aes_encrypt(nonce, appSKey, &AesContext);

            nonce = new byte[16];
            nonce[0] = 0x02;
            nonce.Write(1, appNonce);
            nonce.Write(devNonce, 7);
            appSKey = aes.Encrypt(nonce, 16);
        }
    }
}