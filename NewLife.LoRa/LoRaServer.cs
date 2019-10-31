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
    /// <summary>LoRa服务端</summary>
    public class LoRaServer : NetServer<LoRaSession>
    {
    }

    /// <summary>LoRa会话</summary>
    public class LoRaSession : NetSession
    {
        /// <summary>收到数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
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

        #region 上行数据
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
            //if (pm.FPort > 0 && _appkeys.TryGetValue(pm.DevAddr, out var key))
            //{
            //    var buf = pm.Decrypt(null, key);
            //    WriteLog("解密：{0}", buf.ToHex());
            //}
        }
        #endregion

        #region 下行数据
        /// <summary>拉取数据</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual Packet PullData(Packet data)
        {
            return null;
        }
        #endregion
    }
}