using System;
using System.IO;
using NewLife.Data;
using NewLife.LoRa.Security;
using NewLife.Serialization;

namespace NewLife.LoRa.Messaging;

/// <summary>硬件负载</summary>
/// <remarks>
/// https://www.gitbook.com/book/twowinter/lorawan-specification_zh_cn
/// </remarks>
public class PHYMessage : IAccessor
{
    #region 属性
    /// <summary>MAC层帧头</summary>
    public Byte MHDR { get; set; }

    /// <summary>终端短地址</summary>
    public UInt32 DevAddr { get; set; }

    /// <summary>控制字</summary>
    public Byte FCtrl { get; set; }

    /// <summary>帧计数器</summary>
    /// <remarks>
    /// 上行链路计数器（FCntUp），由终端产生并维护，记录发往服务器的帧数量；
    /// 下行链路计数器（FCntDown），由服务器产生并维护，记录服务器发往终端的帧数量。
    /// </remarks>
    public UInt16 FCnt { get; set; }

    /// <summary>帧配置，字节数不定，最多15字节，大部分情况0个字节</summary>
    /// <remarks>
    /// 一帧数据中可以包含任何MAC命令，MAC命令既可以放在FOpts中，也可以放在FRMPayload中，但不能同时在两个字段携带MAC命令。
    /// MAC命令放在FRMPayload时，FPort = 0。
    /// 放在FOpts的命令不加密（原因：加密Payload，对整个数据签名），也不能超过15个字节（2^4 - 1）。
    /// 放在FRMPayload的MAC命令长度不能超过FRMPayload的最大值。
    /// 不想被别人截获的命令要放到FRMPayload，并单独发送该数据帧一条mac命令由一个命令ID（CID，一个字节），和特定的命令序列组成，命令序列可以是空。
    /// </remarks>
    public Packet FOpts { get; set; }

    /// <summary>MAC数据通道号</summary>
    public Byte FPort { get; set; }

    /// <summary>MAC层负载，加密</summary>
    public Packet Payload { get; set; }

    /// <summary>4字节的校验</summary>
    public UInt32 MIC { get; set; }
    #endregion

    #region 扩展属性
    /// <summary>消息类型</summary>
    public MessageTypes Type { get; set; }

    /// <summary>RFU</summary>
    public Byte RFU { get; set; }

    /// <summary>主版本。0=LoraWan R1</summary>
    public Byte Major { get; set; }

    /// <summary>速率自适应控制</summary>
    public Boolean ADR { get; set; }

    /// <summary>速率自适应控制</summary>
    public Boolean ADRACKReq { get; set; }

    /// <summary>消息确认位。当收到confirmed类型的消息时，进行应答</summary>
    public Boolean ACK { get; set; }

    /// <summary>帧挂起位。</summary>
    /// <remarks>只在下行交互中使用，表示网关还有数据挂起等待下发。此时要求终端尽快发送上行消息来再打开接收窗口。</remarks>
    public Boolean FPending { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载</summary>
    public override String ToString() => $"{GetType().Name}[{Type}, DevAddr={DevAddr:X8}, FCnt={FCnt}, Payload={Payload.Total}]";
    #endregion

    #region 核心读写方法
    /// <summary>从数据流中读取消息</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        var reader = context as BinaryReader ?? new BinaryReader(stream);
        MHDR = reader.ReadByte();
        DevAddr = reader.ReadUInt32();
        FCtrl = reader.ReadByte();
        FCnt = reader.ReadUInt16();

        // MHDR 扩展
        Type = (MessageTypes)((MHDR & 0b_1110_0000) >> 5);
        RFU = (Byte)((MHDR & 0b_0001_1100) >> 2);
        Major = (Byte)((MHDR & 0b_0000_0011) >> 0);

        // FCtrl 扩展
        ADR = (FCtrl & 0b_1000_0000) > 0;
        ADRACKReq = (FCtrl & 0b_0100_0000) > 0;
        ACK = (FCtrl & 0b_0010_0000) > 0;
        FPending = (FCtrl & 0b_0001_0000) > 0;
        var optsLen = FCtrl & 0b_0000_1111;

        if (optsLen > 0) FOpts = reader.ReadBytes(optsLen);
        FPort = reader.ReadByte();

        var dataLen = stream.Length - stream.Position;
        if (dataLen > 4) Payload = stream.ReadBytes(dataLen - 4);

        MIC = reader.ReadUInt32();

        //Console.WriteLine(Payload.ToHex(64));

        return true;
    }

    /// <summary>解密数据</summary>
    /// <param name="nwkSkey"></param>
    /// <param name="appSkey"></param>
    /// <returns></returns>
    public Byte[] Decrypt(Byte[] nwkSkey, Byte[] appSkey)
    {
        var crypto = new LoRaMacCrypto();

        if (FPort == 0)
        {
            if (nwkSkey == null) throw new ArgumentNullException(nameof(nwkSkey));

            return crypto.PayloadDecrypt(Payload.ToArray(), nwkSkey, DevAddr, true, FCnt);
        }
        else
        {
            if (appSkey == null) throw new ArgumentNullException(nameof(appSkey));

            return crypto.PayloadDecrypt(Payload.ToArray(), appSkey, DevAddr, true, FCnt);
        }
    }

    /// <summary>把消息写入到数据流中</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    public virtual Boolean Write(Stream stream, Object context)
    {
        stream.WriteByte(MHDR);
        stream.Write(DevAddr.GetBytes());
        stream.Write((Byte)FCtrl);

        Payload?.CopyTo(stream);

        return true;
    }

    /// <summary>消息转为字节数组</summary>
    /// <returns></returns>
    public virtual Byte[] ToArray()
    {
        var ms = new MemoryStream();
        Write(ms, null);
        return ms.ToArray();
    }

    /// <summary>转数据包</summary>
    /// <returns></returns>
    public virtual Packet ToPacket() => ToArray();
    #endregion

    #region 辅助
    #endregion
}