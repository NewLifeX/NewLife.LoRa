using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.LoRa.Models;

/// <summary>接收数据包</summary>
public class RxPacket
{
    #region 属性
    /// <summary>UTC时间</summary>
    /// <remarks>2013-03-31T16:21:17.528002Z</remarks>
    [XmlElement("time")]
    public DateTime Time { get; set; }

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

    /// <summary>数据速率。SF12BW125</summary>
    [XmlElement("datr")]
    public String DataRate { get; set; }

    /// <summary>编码率</summary>
    [XmlElement("codr")]
    public String CodingRate { get; set; }

    /// <summary>信号强度</summary>
    [XmlElement("rssi")]
    public Int32 RSSI { get; set; }

    /// <summary>Lora SNR ratio in dB</summary>
    [XmlElement("lsnr")]
    public Double Ratio { get; set; }

    /// <summary>负载数据长度</summary>
    [XmlElement("size")]
    public Int32 Length { get; set; }

    /// <summary>数据</summary>
    [XmlElement("data")]
    public String Data { get; set; }
    #endregion

    #region 方法
    /// <summary>读取状态数据</summary>
    /// <param name="data"></param>
    /// <returns>是否成功</returns>
    public static RxPacket[] Read(Object data)
    {
        var list = data as IList<Object>;
        if (list == null) return new RxPacket[0];

        var rs = new List<RxPacket>();
        foreach (var item in list)
        {
            if (item is IDictionary<String, Object> dic)
            {
                var model = JsonHelper.Convert<RxPacket>(dic);

                if (dic["time"] is String st && st.EndsWithIgnoreCase(" UTC")) model.Time = model.Time.ToLocalTime();

                rs.Add(model);
            }
        }

        return rs.ToArray();
    }
    #endregion
}