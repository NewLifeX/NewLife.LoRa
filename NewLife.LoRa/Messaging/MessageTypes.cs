using System;

namespace NewLife.LoRa.Messaging;

/// <summary>MAC消息类型</summary>
public enum MessageTypes : Byte
{
    /// <summary>加网请求</summary>
    JoinRequest = 0b_0000,

    /// <summary>加网通过</summary>
    JoinAccept = 0b_0001,

    /// <summary>不需要确认的数据上报</summary>
    UnconfirmedDataUp = 0b_0010,

    /// <summary>不需要确认的数据下发</summary>
    UnconfirmedDataDown = 0b_0011,

    /// <summary>需要确认的数据上报</summary>
    ConfirmedDataUp = 0b_0100,

    /// <summary>需要确认的数据下发</summary>
    ConfirmedDataDown = 0b_0101,

    /// <summary>保留内容</summary>
    RFU = 0b_0110,

    /// <summary>专有消息，用于实现非标准消息格式</summary>
    Proprietary = 0b_0111,
}
