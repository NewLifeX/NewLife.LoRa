using System;

namespace NewLife.LoRa.Messaging;

/// <summary>消息类型</summary>
public enum LoRaType : Byte
{
    /// <summary>推数据。状态和转包数据</summary>
    PushData = 0,

    /// <summary>推数据确认</summary>
    PushAck = 1,

    /// <summary>拉数据。网关要从服务端拉数据</summary>
    PullData = 2,

    /// <summary>拉数据响应。服务端随时可以发要拉的数据给网关</summary>
    PullResp = 3,

    /// <summary>拉数据确认</summary>
    PullAck = 4,

    /// <summary>发送确认</summary>
    TxAck = 5,
}