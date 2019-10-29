using System;
using System.Collections.Generic;
using NewLife.Agent;
using NewLife.Data;
using NewLife.Log;
using NewLife.LoRa;
using NewLife.LoRa.Messaging;
using NewLife.LoRa.Models;
using NewLife.Serialization;

namespace NewLife.LORAServer
{
    class Program
    {
        static void Main(String[] args) => new MyService().Main();

        class MyService : AgentServiceBase<MyService>
        {
            public MyService()
            {
                ServiceName = "LoRaServer";
                DisplayName = "LoRa服务器";

                AddMenu('t', "测试数据", Test);
            }

            private LoRaServer _Server;
            protected override void StartWork(String reason)
            {
                // 配置
                var set = Setting.Current;

                // 服务器
                var svr = new LoRaServer()
                {
                    Port = set.Port,
                    Log = XTrace.Log,
                };

                if (set.Debug) svr.SessionLog = XTrace.Log;

                svr.Start();

                _Server = svr;

                base.StartWork(reason);
            }

            protected override void StopWork(String reason)
            {
                _Server.TryDispose();
                _Server = null;

                base.StopWork(reason);
            }

            private void Test()
            {
                //var str = \"{\\"stat\\":{\\"time\\":\\"2019-10-25 07:05:33 UTC\\",\\"lati\\":\\"31.231013\\",\\"long\\":\\"121.200607\\",\\"alti\\":\\"30.200000\\",\\"rxnb\\":0,\\"rxok\\":0,\\"rxfw\\":0,\\"ackr\\":100.0,\\"dwnb\\":0,\\"txnb\\":0,\\"batt\\":0,\\"poe\\":0,\\"net\\":1,\\"traffic\\":780539002,\\"ver\\":\\"V3.0.864.862.868_Release\\"}}\";
                var str = "{\"rxpk\":[{\"tmst\":196287580,\"chan\":5,\"rfch\":1,\"freq\":474.100000,\"stat\":1,\"modu\":\"LORA\",\"datr\":\"SF12BW125\",\"codr\":\"4/5\",\"lsnr\":-12.5,\"rssi\":-124,\"size\":50,\"data\":\"gHMAEHCADwsB3P7NADg1Rsj2FLImBtz/9e3hVNzniwoMGUhlyC4KI8Lsvt1VKSuSyVM=\"}]}";
                //var str = "{\"txpk\":{\"imme\":true,\"freq\":864.123456,\"rfch\":0,\"powe\":14,\"modu\":\"LORA\",\"datr\":\"SF11BW125\",\"codr\":\"4/6\",\"ipol\":false,\"size\":32,\"data\":\"H3P3N2i9qc4yt7rK7ldqoeCVJGBybzPY5h1Dd7P7p8v\"}}";
                var js = new JsonParser(str).Decode() as IDictionary<String, Object>;
                //var st = JsonHelper.Convert<StatModel>(js[\"stat\"]);

                //Console.WriteLine(st.ToJson(true));

                var st = StatModel.Read(js["stat"]);
                if (st != null) Console.WriteLine(st.ToJson(true));

                var dt = RxPacket.Read(js["rxpk"]);
                if (dt.Length > 0)
                {
                    Console.WriteLine(dt.ToJson(true));

                    var dp = dt[0];
                    if (!dp.Data.IsNullOrEmpty())
                    {
                        Packet pk = dp.Data.ToBase64();
                        Console.WriteLine(pk.ToHex(64));
                        Console.WriteLine(pk.ToStr());

                        var pm = new PHYMessage();
                        pm.Read(pk.GetStream(), null);

                        Console.WriteLine(pm.ToJson(true));
                    }
                }

                var tx = TxPacket.Read(js["txpk"]);
                if (tx != null) Console.WriteLine(tx.ToJson(true));
            }
        }
    }
}