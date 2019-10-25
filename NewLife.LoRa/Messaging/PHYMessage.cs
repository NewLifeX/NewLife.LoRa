using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.LoRa.Messaging
{
    /// <summary>硬件负载</summary>
    public class PHYMessage : IAccessor
    {
        #region 属性
        /// <summary>MAC层帧头</summary>
        public Byte MHDR { get; set; }

        /// <summary>终端ID</summary>
        public UInt32 DevAddr { get; set; }

        /// <summary>控制字</summary>
        public Byte FCtrl { get; set; }

        /// <summary>序号</summary>
        public UInt16 FCnt { get; set; }

        /// <summary>帧配置，字节数不定，大部分情况0个字节</summary>
        public Packet FOpts { get; set; }

        /// <summary>MAC数据通道号</summary>
        public UInt16 FPort { get; set; }

        /// <summary>MAC层负载，加密</summary>
        public Packet Payload { get; set; }

        /// <summary>4字节的校验</summary>
        public UInt32 MIC { get; set; }
        #endregion

        #region 扩展属性
        /// <summary>消息类型</summary>
        public MacType Type { get; set; }

        /// <summary>主版本</summary>
        public Byte Major { get; set; }
        #endregion

        #region 构造
        /// <summary>已重载</summary>
        public override String ToString() => $"{GetType().Name}[{Type}, DevAddr={DevAddr:X8}, FCnt={FCnt}, Payload={Payload.Total}]";
        #endregion

        #region 核心读写方法
        /// <summary>从数据流中读取消息</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(Stream stream, Object context)
        {
            MHDR = (Byte)stream.ReadByte();
            DevAddr = stream.ReadBytes(4).ToUInt32();
            FCtrl = (Byte)stream.ReadByte();

            Payload = stream.ReadBytes();

            Type = (MacType)(MHDR & 0xE0);
            Major = (Byte)(MHDR & 0x03);

            return true;
        }

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        public virtual Boolean Write(Stream stream, Object context)
        {
            stream.WriteByte(MHDR);
            stream.Write(DevAddr.GetBytes());
            stream.Write((Byte)FCtrl);

            Payload?.CopyTo(stream);

            return true;
        }

        /// <summary>消息转为字节数组</summary>
        /// <returns></returns>
        public virtual Byte[] ToArray()
        {
            var ms = new MemoryStream();
            Write(ms, null);
            return ms.ToArray();
        }

        /// <summary>转数据包</summary>
        /// <returns></returns>
        public virtual Packet ToPacket() => ToArray();
        #endregion

        #region 辅助
        #endregion
    }
}