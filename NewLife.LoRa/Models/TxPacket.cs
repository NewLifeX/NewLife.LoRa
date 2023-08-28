using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.LoRa.Models;

/// <summary>发送数据包</summary>
public class TxPacket
{
    #region 属性
    /// <summary>立即发送，将忽略tmst&time</summary>
    [XmlElement("imme")]
    public Boolean Immediately { get; set; }

    /// <summary>内部时间戳</summary>
    [XmlElement("tmst")]
    public Int32 Timestamp { get; set; }

    /// <summary>GPS时间。1980年1月6日以来毫秒数</summary>
    [XmlElement("tmms")]
    public Int64 GPSTime { get; set; }

    /// <summary>频率。MHz</summary>
    [XmlElement("freq")]
    public Double Frequency { get; set; }

    /// <summary>RF链</summary>
    [XmlElement("rfch")]
    public Int32 Chain { get; set; }

    /// <summary>发射功率dBm</summary>
    [XmlElement("powe")]
    public Int32 Power { get; set; }

    /// <summary>模块标识。LORA/FSK</summary>
    [XmlElement("modu")]
    public String Module { get; set; }

    /// <summary>数据速率。SF12BW125</summary>
    [XmlElement("datr")]
    public String DataRate { get; set; }

    /// <summary>编码率</summary>
    [XmlElement("codr")]
    public String CodingRate { get; set; }

    /// <summary>FSK频率。Hz</summary>
    [XmlElement("fdev")]
    public Int32 FSKFrequency { get; set; }

    /// <summary>Lora极化反转</summary>
    [XmlElement("ipol")]
    public Boolean PolarizationInversion { get; set; }

    /// <summary>前置大小</summary>
    [XmlElement("prea")]
    public Int32 PreambleSize { get; set; }

    /// <summary>负载大小</summary>
    [XmlElement("size")]
    public Int32 Size { get; set; }

    /// <summary>数据</summary>
    [XmlElement("data")]
    public String Data { get; set; }

    /// <summary>禁用Crc</summary>
    [XmlElement("ncrc")]
    public Boolean NoCrc { get; set; }
    #endregion

    #region 方法
    /// <summary>读取状态数据</summary>
    /// <param name="data"></param>
    /// <returns>是否成功</returns>
    public static TxPacket Read(Object data)
    {
        if (data is IDictionary<String, Object> dic)
        {
            var model = JsonHelper.Convert<TxPacket>(dic);

            return model;
        }

        return null;
    }
    #endregion
}