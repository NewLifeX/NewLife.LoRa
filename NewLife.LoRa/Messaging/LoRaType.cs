using System;

namespace NewLife.LoRa.Messaging
{
    /// <summary>消息类型</summary>
    public enum LoRaType : Byte
    {
        /// <summary>推数据</summary>
        PushData = 0,

        /// <summary>推数据确认</summary>
        PushAck = 1,

        /// <summary>拉数据</summary>
        PullData = 2,

        /// <summary>拉数据响应</summary>
        PullResp = 3,

        /// <summary>拉数据确认</summary>
        PullAck = 4,

        /// <summary>发送确认</summary>
        TxAck = 5,

        /// <summary>发布释放（保证交付第二步）</summary>
        PubRel = 6,

        /// <summary>QoS 2消息发布完成（保证交互第三步）</summary>
        PubComp = 7,

        /// <summary>客户端订阅请求</summary>
        Subscribe = 8,

        /// <summary>订阅确认</summary>
        SubAck = 9,

        /// <summary>取消订阅</summary>
        UnSubscribe = 10,

        /// <summary>取消订阅确认</summary>
        UnSubAck = 11,

        /// <summary>Ping请求</summary>
        PingReq = 12,

        /// <summary>Ping响应</summary>
        PingResp = 13,

        /// <summary>客户端断开连接</summary>
        Disconnect = 14,

        ///// <summary>保留</summary>
        //Reserved2 = 15
    }
}