using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.LoRa.Models;

/// <summary>状态包</summary>
public class StatModel
{
    #region 属性
    /// <summary>UTC时间</summary>
    /// <remarks>2013-03-31T16:21:17.528002Z</remarks>
    [XmlElement("time")]
    public DateTime Time { get; set; }

    /// <summary>经度</summary>
    [XmlElement("lati")]
    public Double Latitude { get; set; }

    /// <summary>纬度</summary>
    [XmlElement("long")]
    public Double Longitude { get; set; }

    /// <summary>海拔</summary>
    [XmlElement("alti")]
    public Double Altitude { get; set; }

    /// <summary>接收包数</summary>
    [XmlElement("rxnb")]
    public Int32 ReceivedPackets { get; set; }

    /// <summary>有效包数</summary>
    [XmlElement("rxok")]
    public Int32 ValidPackets { get; set; }

    /// <summary>转发包</summary>
    [XmlElement("rxfw")]
    public Int32 ForwardedPackets { get; set; }

    /// <summary>确认比例</summary>
    [XmlElement("ackr")]
    public Double AcknowledgedPercentage { get; set; }

    /// <summary>下载包数</summary>
    [XmlElement("dwnb")]
    public Int32 Downlinks { get; set; }

    /// <summary>发射包数</summary>
    [XmlElement("txnb")]
    public Int32 EmittedPackets { get; set; }
    #endregion

    #region 扩展属性
    /// <summary>电池</summary>
    [XmlElement("batt")]
    public Int32 Battery { get; set; }

    /// <summary>以太网供电</summary>
    [XmlElement("poe")]
    public Int32 POE { get; set; }

    /// <summary>以太网</summary>
    [XmlElement("net")]
    public Int32 Ethernet { get; set; }

    /// <summary>流量</summary>
    [XmlElement("traffic")]
    public Int32 Traffic { get; set; }

    /// <summary>版本</summary>
    [XmlElement("ver")]
    public String Version { get; set; }
    #endregion

    #region 方法
    /// <summary>读取状态数据</summary>
    /// <param name="data"></param>
    /// <returns>是否成功</returns>
    public static StatModel Read(Object data)
    {
        var dic = data as IDictionary<String, Object>;
        if (dic == null) return null;

        var model = JsonHelper.Convert<StatModel>(dic);

        if (dic["time"] is String st && st.EndsWithIgnoreCase(" UTC")) model.Time = model.Time.ToLocalTime();

        return model;
    }
    #endregion
}
