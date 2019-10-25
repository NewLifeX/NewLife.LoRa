using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.LoRa.Messaging
{
    /// <summary>MAC消息类型</summary>
    public enum MacType : Byte
    {
        /// <summary>加网请求</summary>
        JoinRequest = 0x00,

        /// <summary>加网通过</summary>
        JoinAccept = 0x20,

        /// <summary>不需要确认的数据上报</summary>
        UnconfirmedDataUp = 0x40,

        /// <summary>不需要确认的数据下发</summary>
        UnconfirmedDataDown = 0x60,

        /// <summary>需要确认的数据上报</summary>
        ConfirmedDataUp = 0x80,

        /// <summary>需要确认的数据下发</summary>
        ConfirmedDataDown = 0xA0,

        /// <summary>保留内容</summary>
        RFU = 0xC0,

        /// <summary>专有消息，用于实现非标准消息格式</summary>
        Proprietary = 0xE0,
    }
}
