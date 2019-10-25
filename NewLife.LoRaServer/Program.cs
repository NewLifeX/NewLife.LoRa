using System;
using System.Collections.Generic;
using NewLife.Agent;
using NewLife.Log;
using NewLife.LoRa;
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
                var stat = "{\"stat\":{\"time\":\"2019-10-25 07:05:33 UTC\",\"lati\":\"31.231013\",\"long\":\"121.200607\",\"alti\":\"30.200000\",\"rxnb\":0,\"rxok\":0,\"rxfw\":0,\"ackr\":100.0,\"dwnb\":0,\"txnb\":0,\"batt\":0,\"poe\":0,\"net\":1,\"traffic\":780539002,\"ver\":\"V3.0.864.862.868_Release\"}}";
                var js = new JsonParser(stat).Decode() as IDictionary<String, Object>;
                //var st = JsonHelper.Convert<StatModel>(js["stat"]);

                //Console.WriteLine(st.ToJson(true));

                var st = StatModel.Read(js["stat"]);
                if (st != null) Console.WriteLine(st.ToJson(true));
            }
        }
    }
}