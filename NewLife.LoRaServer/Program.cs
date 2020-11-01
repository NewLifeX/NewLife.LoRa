using System;
using System.Collections.Generic;
using System.Diagnostics;
using NewLife.Agent;
using NewLife.Data;
using NewLife.Log;
using NewLife.LoRa;
using NewLife.LoRa.Messaging;
using NewLife.LoRa.Models;
using NewLife.LoRa.Security;
using NewLife.Serialization;

namespace NewLife.LORAServer
{
    class Program
    {
        static void Main(String[] args) => new MyService().Main(args);

        class MyService : ServiceBase
        {
            public MyService()
            {
                ServiceName = "LoRaServer";
                DisplayName = "LoRa服务器";

                AddMenu('t', "测试数据", Test);
                AddMenu('s', "测试加密", Test2);
                AddMenu('d', "测试解密", Test3);
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
                //var str = "{\"rxpk\":[{\"tmst\":196287580,\"chan\":5,\"rfch\":1,\"freq\":474.100000,\"stat\":1,\"modu\":\"LORA\",\"datr\":\"SF12BW125\",\"codr\":\"4/5\",\"lsnr\":-12.5,\"rssi\":-124,\"size\":50,\"data\":\"gHMAEHCADwsB3P7NADg1Rsj2FLImBtz/9e3hVNzniwoMGUhlyC4KI8Lsvt1VKSuSyVM=\"}]}";
                var str = "{\"rxpk\":[{\"tmst\":438505452,\"chan\":2,\"rfch\":0,\"freq\":473.500000,\"stat\":1,\"modu\":\"LORA\",\"datr\":\"SF12BW125\",\"codr\":\"4/5\",\"lsnr\":-14.0,\"rssi\":-119,\"size\":50,\"data\":\"gGEAEHCACwABRaWG3UZsomFqt4sxJmt0JGNFCS3PWweysY1Vi+94PmFTmoycmDxCviA=\"}]}";
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

                        var nwkSkey = "4B463EFED018F099FE3F05108618FDDA".ToHex();
                        //var appSkey = "19E52095515EBD0C2FD596DD96FD0833".ToHex();
                        var appSkey = "778960777F7B4CBAC857C06DEE818844".ToHex();
                        var buf = pm.Decrypt(nwkSkey, appSkey);
                        Console.WriteLine(buf.ToHex());
                        Console.WriteLine(buf.ToStr());
                    }
                }

                var tx = TxPacket.Read(js["txpk"]);
                if (tx != null) Console.WriteLine(tx.ToJson(true));
            }

            private void Test2()
            {
                var ss = new[] {
                "gGMAEHCAGAcB0kSc/G+ehD4Z93QxnfV6P0i0dO2SSrCsl5ZdXI8rRQzT3W3Ej1mHTO8=",
                "gLcAEHCANQ4BsLzPNq3dJKRyAtgGjfP0kyQgN4RWzRAcO0LPfU1J0L6VszQJGwANg7Y=",
                "gFQAEHCAWgMBpcwy1MAkLGFPRII9hUlB4+3+O1d0p2ZOgsHu21BAikAOYtjTh60Dx+s=",
                "gHMAEHCAEAsBcdf9lNe4FL+Hvs8lUNpie7u10tSOAWqiPyQVyLTx6BbQDWehC6qQa+0=",
                "gHMAEHCADwsB3P7NADg1Rsj2FLImBtz/9e3hVNzniwoMGUhlyC4KI8Lsvt1VKSuSyVM=",
                "gF0AEHCATgQBdf0x8gNiz9fqC13IfE79yqd4SyMtTDyk02gQoW317HwJ6L1zt8rXAIc=",
                "gF0AEHCATwQB/F5MgTRSRaqVjS9SZjt1rQYdPtl4hSL2Tox4Y8TRW4yatCH/7l75/Q4=",
                };
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

                foreach (var item in ss)
                {
                    Console.WriteLine();
                    Packet pk = item.ToBase64();

                    var pm = new PHYMessage();
                    pm.Read(pk.GetStream(), null);
                    Console.WriteLine(pm.Payload.ToHex(64));

                    var addr = pm.DevAddr.ToString("X8");
                    //Console.WriteLine(pm.Type);
                    Console.WriteLine("{0} {1} FCnt={2} FPort={3}", addr, pm.Type, pm.FCnt, pm.FPort);
                    //Console.WriteLine(pm.ToJson(true));

                    //var nwkSkey = "4B463EFED018F099FE3F05108618FDDA".ToHex();
                    //var appSkey = "19E52095515EBD0C2FD596DD96FD0833".ToHex();
                    var appSkey = dic[addr].ToHex();
                    var buf = pm.Decrypt(null, appSkey);
                    Console.WriteLine(buf.ToHex());
                    //Console.WriteLine(buf.ToStr());
                }

                //var crypto = new LoRaMacCrypto();

                //var buf = "C86B3BF3".ToHex();
                //var key = "A499E0B73311D0782EC80C98FEC83B8E".ToHex();
                //var rs = crypto.PayloadDecrypt(buf, key, 0x77F7EEF0, true, 0x20);

                //var str = rs.ToHex();
                //XTrace.WriteLine(str);
                //Debug.Assert(str == "B93747B2");
            }

            private void Test3()
            {
                var crypto = new LoRaMacCrypto();

                //var buf = "C86B3BF3".ToHex();
                var buf = "58E1369B".ToHex();
                var key = "A499E0B73311D0782EC80C98FEC83B8E".ToHex();
                var rs = crypto.PayloadDecrypt(buf, key, 0x77F7EEF0, true, 0x0142);

                var str = rs.ToHex();
                XTrace.WriteLine(str);
                //Debug.Assert(str == "B93747B2");
                Debug.Assert(str == "092200DB");
            }
        }
    }
}