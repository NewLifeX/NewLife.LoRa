using System;
using System.IO;

namespace NewLife.LoRa.Security;

/// <summary>LoRaMac加解密</summary>
public class LoRaMacCrypto
{
    #region 属性
    #endregion

    /// <summary>计算 LoRaMAC 的 MIC</summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="key">AES密钥</param>
    /// <param name="address">帧地址</param>
    /// <param name="dir">方向，0上行，1下行</param>
    /// <param name="sequenceCounter">帧序号计数器</param>
    public UInt32 ComputeMic(Byte[] buffer, Byte[] key, UInt32 address, Boolean dir, UInt32 sequenceCounter)
    {
        var size = buffer.Length;

        var block = new Byte[] { 0x49, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        block[5] = (Byte)(dir ? 1 : 0);

        block[6] = (Byte)((address) & 0xFF);
        block[7] = (Byte)((address >> 8) & 0xFF);
        block[8] = (Byte)((address >> 16) & 0xFF);
        block[9] = (Byte)((address >> 24) & 0xFF);

        block[10] = (Byte)((sequenceCounter) & 0xFF);
        block[11] = (Byte)((sequenceCounter >> 8) & 0xFF);
        block[12] = (Byte)((sequenceCounter >> 16) & 0xFF);
        block[13] = (Byte)((sequenceCounter >> 24) & 0xFF);

        block[15] = (Byte)(size & 0xFF);

        var ctx = new AES_CMAC_CTX();
        ctx.Init();
        ctx.SetKey(key);
        ctx.Update(block);
        ctx.Update(buffer);

        return ctx.Final();
    }

    /// <summary>加密</summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="key">AES密钥</param>
    /// <param name="address">帧地址</param>
    /// <param name="dir">方向，0上行，1下行</param>
    /// <param name="sequenceCounter">帧序号计数器</param>
    public Byte[] PayloadEncrypt(Byte[] buffer, Byte[] key, UInt32 address, Boolean dir, UInt32 sequenceCounter)
    {
        var size = buffer.Length;

        var aes = new Aes128();
        aes.SetKey(key);

        //using var aes = Aes.Create();
        //using var aes = new RijndaelManaged();
        //aes.Key = key;
        //aes.IV = new Byte[16];
        //aes.Mode = CipherMode.ECB;
        //aes.Padding = PaddingMode.Zeros;

        //XTrace.WriteLine("{0} {1}", aes.Mode, aes.Padding);

        //var dec = aes.CreateDecryptor();

        var aBlock = new Byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        //var sBlock = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        aBlock[5] = (Byte)(dir ? 0 : 1);

        aBlock[6] = (Byte)((address) & 0xFF);
        aBlock[7] = (Byte)((address >> 8) & 0xFF);
        aBlock[8] = (Byte)((address >> 16) & 0xFF);
        aBlock[9] = (Byte)((address >> 24) & 0xFF);

        aBlock[10] = (Byte)((sequenceCounter) & 0xFF);
        aBlock[11] = (Byte)((sequenceCounter >> 8) & 0xFF);
        aBlock[12] = (Byte)((sequenceCounter >> 16) & 0xFF);
        aBlock[13] = (Byte)((sequenceCounter >> 24) & 0xFF);
        //XTrace.WriteLine("aBlock: {0}", aBlock.ToHex());

        var encBuffer = new Byte[size];
        Byte bufferIndex = 0;
        UInt16 ctr = 1;
        //var sBlock = new Byte[16];
        while (size >= 16)
        {
            aBlock[15] = (Byte)((ctr) & 0xFF);
            ctr++;
            //aes_encrypt(aBlock, sBlock, &AesContext);
            var sBlock = aes.Encrypt(aBlock, 16);
            //dec.TransformBlock(aBlock, 0, aBlock.Length, sBlock, sBlock.Length);
            for (var i = 0; i < 16; i++)
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
            var sBlock = aes.Encrypt(aBlock, 16);
            //sBlock = dec.TransformFinalBlock(aBlock, 0, aBlock.Length);
            for (var i = 0; i < size; i++)
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
    public Byte[] PayloadDecrypt(Byte[] buffer, Byte[] key, UInt32 address, Boolean dir, UInt32 sequenceCounter) => PayloadEncrypt(buffer, key, address, dir, sequenceCounter);

    /// <summary>计算组网校验码</summary>
    /// <param name="buffer"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public UInt32 JoinComputeMic(Byte[] buffer, Byte[] key)
    {
        var ctx = new AES_CMAC_CTX();
        ctx.Init();
        ctx.SetKey(key);
        ctx.Update(buffer);

        return ctx.Final();
    }

    /// <summary>组网解密</summary>
    /// <param name="buffer"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public Byte[] JoinDecrypt(Byte[] buffer, Byte[] key)
    {
        var size = buffer.Length;

        var aes = new Aes128();
        aes.SetKey(key);
        var decBuffer = aes.Encrypt(buffer, 16);

        //memset1(AesContext.ksch, '\0', 240);
        //aes_set_key(key, 16, &AesContext);
        //aes_encrypt(buffer, decBuffer, &AesContext);
        // Check if optional CFList is included
        if (size >= 16)
        {
            //aes_encrypt(buffer + 16, decBuffer + 16, &AesContext);
            var decBuffer2 = aes.Encrypt(buffer.ReadBytes(16, -1), 16);
            //decBuffer = decBuffer.Combine(decBuffer2);
            var ms = new MemoryStream();
            ms.Write(decBuffer);
            ms.Write(decBuffer2);
            decBuffer = ms.ToArray();
        }

        return decBuffer;
    }

    /// <summary>组网加密</summary>
    /// <param name="buffer"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public Byte[] JoinEncrypt(Byte[] buffer, Byte[] key)
    {
        var size = buffer.Length;

        var aes = new Aes128();
        aes.SetKey(key);
        var decBuffer = aes.Decrypt(buffer, 16);

        //memset1(AesContext.ksch, '\0', 240);
        //aes_set_key(key, 16, &AesContext);
        //aes_decrypt(buffer, encBuffer, &AesContext);
        // Check if optional CFList is included
        if (size >= 16)
        {
            //aes_decrypt(buffer + 16, encBuffer + 16, &AesContext);
            var decBuffer2 = aes.Decrypt(buffer.ReadBytes(16, -1), 16);
            //decBuffer = decBuffer.Combine(decBuffer2);
            var ms = new MemoryStream();
            ms.Write(decBuffer);
            ms.Write(decBuffer2);
            decBuffer = ms.ToArray();
        }

        return decBuffer;
    }

    /// <summary>组网计算密钥</summary>
    /// <param name="key"></param>
    /// <param name="appNonce"></param>
    /// <param name="devNonce"></param>
    /// <param name="nwkSKey"></param>
    /// <param name="appSKey"></param>
    public void JoinComputeSKeys(Byte[] key, Byte[] appNonce, UInt16 devNonce, out Byte[] nwkSKey, out Byte[] appSKey)
    {
        var aes = new Aes128();
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

        var nonce = new Byte[16];
        nonce[0] = 0x01;
        nonce.Write(1, appNonce);
        nonce.Write(devNonce, 7);
        nwkSKey = aes.Encrypt(nonce, 16);

        //memset1(nonce, 0, sizeof(nonce));
        //nonce[0] = 0x02;
        //memcpy1(nonce + 1, appNonce, 6);
        //memcpy1(nonce + 7, pDevNonce, 2);
        //aes_encrypt(nonce, appSKey, &AesContext);

        nonce = new Byte[16];
        nonce[0] = 0x02;
        nonce.Write(1, appNonce);
        nonce.Write(devNonce, 7);
        appSKey = aes.Encrypt(nonce, 16);
    }
}