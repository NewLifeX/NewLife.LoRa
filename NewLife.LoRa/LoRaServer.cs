using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    }
}