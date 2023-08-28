using System;

namespace NewLife.LoRa.Security;

class AES_CMAC_CTX
{
    public Aes128 rijndael;

    /// <summary>16</summary>
    public Byte[] X;

    /// <summary>16</summary>
    public Byte[] M_last;

    public Byte M_n;

    public void Init()
    {

    }

    public void SetKey(Byte[] key)
    {

    }

    public void Update(Byte[] data)
    {

    }

    public UInt32 Final()
    {
        return 0;
    }
}

class CMac
{


}