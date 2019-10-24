using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NewLife.LoRa.Models
{
    /// <summary>节点包</summary>
    public class NodePacket
    {
        #region 属性
        /// <summary>UTC时间</summary>
        /// <remarks>2013-03-31T16:21:17.528002Z</remarks>
        [XmlElement("time")]
        public String UTCTime { get; set; }

        /// <summary>GPS时间。1980年1月6日以来毫秒数</summary>
        [XmlElement("tmms")]
        public Int64 GPSTime { get; set; }

        /// <summary>内部时间戳</summary>
        [XmlElement("tmst")]
        public Int32 Timestamp { get; set; }

        /// <summary>频率。MHz</summary>
        [XmlElement("freq")]
        public Double Frequency { get; set; }

        /// <summary>通道</summary>
        [XmlElement("chan")]
        public Int32 Channel { get; set; }

        /// <summary>RF链</summary>
        [XmlElement("rfch")]
        public Int32 Chain { get; set; }

        /// <summary>状态</summary>
        [XmlElement("stat")]
        public Int32 Stat { get; set; }

        /// <summary>模块标识。LORA/FSK</summary>
        [XmlElement("modu")]
        public String Module { get; set; }

        /// <summary>数据速率</summary>
        [XmlElement("datr")]
        public String DataRate { get; set; }

        /// <summary>编码率</summary>
        [XmlElement("codr")]
        public String CodingRate { get; set; }

        /// <summary>信号强度</summary>
        [XmlElement("rssi")]
        public Int32 RSSI { get; set; }

        /// <summary>Lora SNR ratio in dB</summary>
        [XmlElement("isnr")]
        public Double Ratio { get; set; }

        /// <summary>负载数据长度</summary>
        [XmlElement("size")]
        public Int32 Length { get; set; }

        /// <summary>数据速率</summary>
        [XmlElement("data")]
        public String Data { get; set; }
        #endregion
    }
}