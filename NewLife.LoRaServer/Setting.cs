using System;
using System.ComponentModel;
using NewLife.Xml;

namespace NewLife.LORAServer
{
    [XmlConfigFile(@"Config\LoRa.config", 10_000)]
    public class Setting : XmlConfig<Setting>
    {
        /// <summary>调试开关。默认 false</summary>
        [Description("调试开关。默认 false")]
        public Boolean Debug { get; set; }

        /// <summary>端口。默认 1680</summary>
        [Description("端口。默认 1680")]
        public Int32 Port { get; set; } = 1680;
    }
}