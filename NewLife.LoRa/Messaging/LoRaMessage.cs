using System;
using System.IO;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.LoRa.Messaging;

/// <summary>LoRa消息</summary>
/// <remarks>
/// https://github.com/Lora-net/packet_forwarder/blob/master/PROTOCOL.TXT
/// </remarks>
public class LoRaMessage : IAccessor
{
    #region 属性
    /// <summary>版本</summary>
    public Byte Version { get; set; } = 2;

    /// <summary>随机令牌，请求响应配对</summary>
    public UInt16 Token { get; set; }

    /// <summary>命令。PUSH_DATA=0/PUSH_ACK=2</summary>
    public LoRaType Command { get; set; }

    /// <summary>网关MAC地址</summary>
    public UInt64 Mac { get; set; }

    /// <summary>负载</summary>
    public Packet Payload { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载</summary>
    public override String ToString() => $"{GetType().Name}[Command={Command}, Mac={Mac:X16}, Token={Token:X4}, Payload={Payload.Total}]";
    #endregion

    #region 核心读写方法
    /// <summary>从数据流中读取消息</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        Version = (Byte)stream.ReadByte();
        Token = stream.ReadBytes(2).ToUInt16();
        Command = (LoRaType)stream.ReadByte();
        Mac = stream.ReadBytes(8).ToUInt64();

        Payload = stream.ReadBytes(-1);

        return true;
    }

    /// <summary>把消息写入到数据流中</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    public virtual Boolean Write(Stream stream, Object context)
    {
        stream.WriteByte(Version);
        stream.Write(Token.GetBytes());
        stream.Write((Byte)Command);
        stream.Write(Mac.GetBytes());

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
    /// <summary>创建响应消息</summary>
    /// <returns></returns>
    public LoRaMessage CreateReply()
    {
        var rs = new LoRaMessage
        {
            //Version = Version,
            Token = Token,
        };

        switch (Command)
        {
            case LoRaType.PushData: rs.Command = LoRaType.PushAck; break;
            case LoRaType.PushAck:
                break;
            case LoRaType.PullData: rs.Command = LoRaType.PullAck; break;
            case LoRaType.PullResp:
                break;
            case LoRaType.PullAck:
                break;
            case LoRaType.TxAck:
                break;
            default:
                break;
        }

        return rs;
    }
    #endregion
}