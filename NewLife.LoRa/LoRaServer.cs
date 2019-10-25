using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.LoRa.Messaging;
using NewLife.Net;

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
        /// <summary>收到数据</summary>
        /// <param name="e"></param>
        protected override void OnReceive(ReceivedEventArgs e)
        {
            //base.OnReceive(e);

            var msg = new LoRaMessage();
            msg.Read(e.Packet.GetStream(), null);

            WriteLog("{0:X4} {1}", msg.Token, msg.Payload.ToStr());

            var rs = msg.CreateReply();

            Send(rs.ToPacket());
        }
    }
}