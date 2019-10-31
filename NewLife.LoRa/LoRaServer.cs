using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
using NewLife.LoRa.Messaging;
using NewLife.LoRa.Models;
using NewLife.Net;
using NewLife.Serialization;

namespace NewLife.LoRa
{
    /// <summary>MQTT服务端</summary>
    public class LoRaServer : NetServer<LoRaSession>
    {
        /// <summary>启动</summary>
        protected override void OnStart()
        {
            //Add(new MqttCodec());

            base.OnStart();
        }
    }

    /// <summary>会话</summary>
    public class LoRaSession : NetSession
    {
        private static IDictionary<UInt32, Byte[]> _appkeys;

        static LoRaSession()
        {
            var dic = new Dictionary<String, String>
            {
                ["701000B7"] = "5F6C965F3AA482AF2EF8C3FBF63661FE",
                ["70100063"] = "1BA6731021ED686C3643756311DD23CC",
                ["701000A4"] = "ED347BE6FDDF2BCF749354694285841D",
                ["7010005D"] = "8598B09A8CD56BC67AA55C08CEDC183E",
                ["70100073"] = "19E52095515EBD0C2FD596DD96FD0833",
                ["70100054"] = "53BBDC505119EB63BCB17CD15B24AD45",
                ["70100061"] = "778960777F7B4CBAC857C06DEE818844",
                ["701000B1"] = "35D43942B95DC82B80A79BC4BAD9457E",
            };

            _appkeys = dic.ToDictionary(e => e.Key.ToHex().ToUInt32(0, false), e => e.Value.ToHex());
        }

        /// <summary>收到数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            //base.OnReceive(e);

            var msg = new LoRaMessage();
            msg.Read(e.Packet.GetStream(), null);

            WriteLog("{0,-9}<= {1}", msg.Command, msg.Payload?.ToStr());

            switch (msg.Command)
            {
                case LoRaType.PushData: PushData(msg.Payload); break;
                case LoRaType.PushAck:
                    break;
                case LoRaType.PullData:
                    break;
                case LoRaType.PullResp:
                    break;
                case LoRaType.PullAck:
                    break;
                case LoRaType.TxAck:
                    break;
                default:
                    break;
            }

            var rs = msg.CreateReply();
            WriteLog("{0,-9}=> {1}", rs.Command, rs.Payload?.ToStr());

            Send(rs.ToPacket());
        }

        /// <summary>上行数据</summary>
        /// <param name="data"></param>
        protected virtual void PushData(Packet data)
        {
            var str = data.ToStr();
            var js = new JsonParser(str).Decode() as IDictionary<String, Object>;
            //var st = JsonHelper.Convert<StatModel>(js[\"stat\"]);

            var st = StatModel.Read(js["stat"]);
            if (st != null) Console.WriteLine(st.ToJson(true));

            var rxs = RxPacket.Read(js["rxpk"]);
            if (rxs.Length > 0) OnPushPacket(rxs, js);

            //var tx = TxPacket.Read(js["txpk"]);
            //if (tx != null) Console.WriteLine(tx.ToJson(true));
        }

        /// <summary>上行数据包</summary>
        /// <param name="packets"></param>
        /// <param name="ext"></param>
        protected virtual void OnPushPacket(RxPacket[] packets, IDictionary<String, Object> ext)
        {
            foreach (var item in packets)
            {
                if (!item.Data.IsNullOrEmpty())
                {
                    Packet pk = item.Data.ToBase64();

                    var pm = new PHYMessage();
                    pm.Read(pk.GetStream(), null);

                    var addr = pm.DevAddr.ToString("X8");
                    WriteLog("{0} {1} {2}", addr, pm.Type, pm.FCnt);

                    OnPushMessage(pm, item, ext);
                }
            }
        }

        /// <summary>上行消息</summary>
        /// <param name="pm"></param>
        /// <param name="packet"></param>
        /// <param name="ext"></param>
        protected virtual void OnPushMessage(PHYMessage pm, RxPacket packet, IDictionary<String, Object> ext)
        {
            if (pm.FPort > 0 && _appkeys.TryGetValue(pm.DevAddr, out var key))
            {
                var buf = pm.Decrypt(null, key);
                WriteLog("解密：{0}", buf.ToHex());
            }
        }
    }
}