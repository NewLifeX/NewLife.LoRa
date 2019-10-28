using System;
using System.IO;
using System.Text;
using NewLife.Log;

namespace NewLife.LoRa.Messaging
{
    /// <summary>数据消息</summary>
    public class DataMessage
    {
        #region 数据包格式
        // 字节数
        // Preamble  PHDR  PHDR_CRC  PHYPayload  CRC
        // PHYPayload: MHDR(1 byte)  MACPayload  MIC(4 byte)
        // MACPayload: FHDR  FPort(0 or 1 byte)  FRMPayload(n byte)
        // FHDR: DevAddr(4 byte)  FCtrl(1 byte)  FCnt(2 byte)  FOpts(n byte)

        // 数据包展开看  去掉硬件部分，数据如下。
        // MHDR(1 byte)  DevAddr(4 byte)  FCtrl(1 byte)  FCnt(2 byte)  CmdPlayLoad(0 - 15 byte)  FPort(0 or 1 byte)  FRMPayload(n byte)  MIC(4 byte)

        // 消息另外一种形式
        // 没有  FRMPayload 时，FCtrl 中 FOptsLen = 0 ， FPort = 0
        // MHDR(1 byte)  DevAddr(4 byte)  FCtrl(1 byte)  FCnt(2 byte)  FPort(0 or 1 byte)  CmdPlayLoad(0 - 15 byte)  MIC(4 byte)

        // 无负载数据的消息  有校验，无加密
        // MHDR(1 byte)  DevAddr(4 byte)  FCtrl(1 byte)  FCnt(2 byte)  MIC(4 byte)

        // 校验数据是 除 MIC 之外的所有，  使用 NwkSkey
        // 加密数据 一定是  （FPort ， MIC） 之间的数据。  FPort = 0时使用NwkSkey  其余使用 AppSKey

        // MSB 高位在前描述
        // MHDR: MType(3bit) + RFU(3bit) + Major(2bit)
        // FCtrl: ADR(1bit) + ADRACKReq(1bit) + ACK(1bit) + RFU(1bit) or FPending(1bit) + FOptsLen(4bit)
        #endregion

        #region 数据包头部内容

        /// <summary>设备地址</summary>
        public Int32 DevAddr { get; set; } = 0;

        /// <summary>ADR （速率自适应）标志，标识服务器是否可以修改节点的传输速率</summary>
        public Boolean ADR = false;

        /// <summary>节点请求服务器修改服务器发送的传输参数。表示节点网络质量差，有重传超限，需要 ADR 介入</summary>
        /// <remarks>节点没有收到有效数据时AdrAckCounter++，当AdrAckCounter >= CN470_ADR_ACK_LIMIT，节点将Txpower提到最高
        /// 当AdrAckCounter >= ( CN470_ADR_ACK_LIMIT + CN470_ADR_ACK_DELAY )时，节点用大DR发送数据</remarks>
        public Boolean ADRACKReq = false;

        /// <summary>数据ACK位，响应confirmed数据用</summary>
        public Boolean ACK = false;

        /// <summary>保留位</summary>
        public Boolean RfuFlag = false;

        /// <summary>帧挂起位。下行数据时使用，表示网关还有数据需要下发，让节点尽快上行数据。（Class A&B 只能节点发起通讯）</summary>
        public Boolean FPending = false;

        private Byte _FOptsLen = 0;
        /// <summary>扩展项长度，扩展项是MAC命令，最长15字节</summary>
        public Byte FOptsLen
        {
            get => _FOptsLen;
            set
            {
                if (value > 0x0f) return;
                _FOptsLen = value;
            }
        }

        /// <summary>FCtrl</summary>
        private Byte FCtrl
        {
            get
            {
                var rs = _FOptsLen;
                if (ADR) rs |= 0x80;
                if (ADRACKReq) rs |= 0x40;
                if (ACK) rs |= 0x20;

                switch (MTpye)
                {
                    case MType_e.UnconfirmedDataUp:
                    case MType_e.ConfirmedDataUp:
                        if (RfuFlag) rs |= 0x10;
                        break;
                    case MType_e.UnconfirmedDataDown:
                    case MType_e.ConfirmedDataDown:
                        if (FPending) rs |= 0x10;
                        break;

                    case MType_e.JoinRequest: break;
                    case MType_e.JoinAccept: break;
                    case MType_e.RFU: break;
                    case MType_e.Proprietary: break;
                    default: break;
                }

                return rs;
            }
            set
            {
                _FOptsLen = (Byte)(value & 0x0f);

                ADR = (value & 0x80) == 0x80;
                ADRACKReq = (value & 0x40) == 0x40;
                ACK = (value & 0x20) == 0x20;

                switch (MTpye)
                {
                    case MType_e.UnconfirmedDataUp:
                    case MType_e.ConfirmedDataUp:
                        RfuFlag = (value & 0x10) == 0x10;
                        break;
                    case MType_e.UnconfirmedDataDown:
                    case MType_e.ConfirmedDataDown:
                        FPending = (value & 0x10) == 0x10;
                        break;

                    case MType_e.JoinRequest: break;
                    case MType_e.JoinAccept: break;
                    case MType_e.RFU: break;
                    case MType_e.Proprietary: break;
                    default: break;
                }
            }
        }

        /// <summary>FCnt</summary>
        public UInt16 FCnt = 0x00;

        private Byte _FPort = 0;
        /// <summary>  如果有效载荷不为0，则FPort必须存在。 0-223 是可用段，224  225保留用于以后扩展。 </summary>
        public Byte FPort
        {
            get => _FPort;
            set
            {
                if (value < 0) { XTrace.WriteLine("FPort 超出范围"); return; }
                if (value > 255) { XTrace.WriteLine("FPort 超出范围"); return; }
                if (value > 233) XTrace.WriteLine("注意，这是保留端口");
                _FPort = value;
            }
        }

        #endregion

        #region 负载数据

        /// <summary>正儿八经的负载数据</summary>
        public Byte[] Payload;

        /// <summary>扩展项内容，MAC命令数据</summary>
        public Byte[] CmdPayload;

        #endregion

        #region 秘钥

        /// <summary>网络秘钥 16字节</summary>
        public Byte[] NwkSKey { get; set; } = null;

        /// <summary>应用秘钥 16字节</summary>
        public Byte[] AppSKey { get; set; } = null;

        #endregion

        #region 方法

        /// <summary>从字节数组读取消息内容</summary>
        /// <param name="bs"></param>
        public void Read(Byte[] bs)
        {
            if (bs.Length < 12)
            {
                XTrace.WriteLine("消息长度错误");
                Valid = false;
                return;
            }

            Buffer = bs;

            // XTrace.WriteLine("DataMessage {0}", bs.ToHex());
            var st = new MemoryStream(bs);

            MHDR = (Byte)st.ReadByte();
            DevAddr = (Int32)st.ReadBytes(4).ToUInt32();
            FCtrl = (Byte)st.ReadByte();
            FCnt = st.ReadBytes(2).ToUInt16();

            // 最后4字节是MIC  直接提取出来，比Steam里面拿简单暴力有效。
            MIC = bs.ReadBytes(bs.Length - 4, 4).ToUInt32();

            // 附带数据
            if (FOptsLen != 0)
            {
                CmdPayload = st.ReadBytes(FOptsLen);
                // XTrace.WriteLine("cmdpayload  " + CmdPayload.ToHex());
            }

            var remian = st.Capacity - st.Position;
            //  Fport(1byte) + 负载(n byte) + MIC(4 byte)
            if (remian > 4)
            {
                FPort = (Byte)st.ReadByte();

                // 除了尾巴MIC 之外都是负载数据
                var buf = st.ReadBytes();
                var data = buf.ReadBytes(0, buf.Length - 4);

                // XTrace.WriteLine("FPort " + FPort + " remian " + (remian - 5) + "  " + data.ToHex());

                if (FPort == 0)
                    CmdPayload = data;
                else
                    Payload = data;
            }

        }

        /// <summary>序列化包</summary>
        /// <returns></returns>
        public Byte[] ToArray()
        {
            if (Buffer == null) Build();

            return Buffer;
        }

        /// <summary>创建消息</summary>
        public void Build()
        {
            if ((NwkSKey == null) || (NwkSKey.Length != 16)) XTrace.WriteLine("NwkSkey 不满足要求");
            if ((AppSKey == null) || (AppSKey.Length != 16)) XTrace.WriteLine("AppSKey 不满足要求");

            // 加密数据
            Encrypt(NwkSKey, AppSKey);

            var ms = new MemoryStream();

            ms.Write(MHDR);
            ms.Write(DevAddr.GetBytes());

            // CmdPayload 放到 FPort=0 后面。
            var cmdaffterFport = false;
            // 没有 CmdPayload 长度一定是 0
            if ((CmdPayload == null) || (CmdPayload.Length == 0))
            {
                FOptsLen = 0;
            }
            // 只有 CmdPayload 没有 Payload 长度也是0
            else
            {
                FOptsLen = (Byte)CmdPayload.Length;
                if ((Payload == null) || (Payload.Length == 0))
                {
                    FOptsLen = 0;
                    FPort = 0;
                    cmdaffterFport = true;
                }
            }

            // XTrace.WriteLine("FCtrl {0}", FCtrl.ToString("X2"));
            ms.Write(FCtrl);
            ms.Write(FCnt.GetBytes());

            if (cmdaffterFport)
            {
                ms.Write(FPort);
                ms.Write(CmdPayload);
            }
            else
            {
                if (CmdPayload != null) ms.Write(CmdPayload);

                if (Payload != null)
                {
                    ms.Write(FPort);
                    ms.Write(Payload);
                }
            }
            // 创建的消息一定是下行的！
            LoRaMacCrypto.LoRaMacComputeMic(ms.ToArray(), NwkSKey, DevAddr, false, FCnt, ref MIC);

            ms.Write(MIC.GetBytes());

            Buffer = ms.ToArray();
        }

        /// <summary>使用key去校验数据合法性</summary>
        /// <param name="nwkSkey">AppEui对应的key</param>
        /// <returns>校验结果</returns>
        public Boolean ValidationData(Byte[] nwkSkey)
        {
            if (!Valid) return false;

            if (nwkSkey == null) new NullReferenceException("nwkSkey Error");

            var data = Buffer.ReadBytes(0, Buffer.Length - 4);

            UInt32 mic = 0;
            LoRaMacCrypto.LoRaMacComputeMic(data, nwkSkey, DevAddr, true, FCnt, ref mic);
            if (mic == MIC) return true;

            XTrace.WriteLine("数据包校验失败，MIC 计算值 {0} 包内值 {1} ", mic.GetBytes().ToHex(), MIC.GetBytes().ToHex());
            return false;
        }

        /// <summary>解密信息</summary>
        /// <param name="key"></param>
        public void Decrypt(Byte[] nwkSkey = null, Byte[] appSkey = null)
        {
            // XTrace.WriteLine("DataMessage Decrypt");
            if (nwkSkey == null) new NullReferenceException("nwkSkey Error");
            if (appSkey == null) new NullReferenceException("appSkey Error");

            // 没有数据需要解密
            if (((CmdPayload == null) || (CmdPayload.Length == 0)) && ((Payload == null) || (Payload.Length == 0))) return;

            // 没有 Payload ，有 CmdPayload 的时候    FPort 一定等于 0
            // if ((FPort == 0)&& ((Payload == null) || (Payload.Length == 0)))
            if (((Payload == null) || (Payload.Length == 0)))
            {
                // XTrace.WriteLine("Decrypt 1");
                var data = new Byte[CmdPayload.Length];
                LoRaMacCrypto.LoRaMacPayloadDecrypt(CmdPayload, nwkSkey, DevAddr, true, FCnt, data);
                CmdPayload = data;

                // XTrace.WriteLine("明文数据 CmdPayload " + CmdPayload.ToHex());
            }
            else
            {
                // XTrace.WriteLine("Decrypt 2");
                var data = new Byte[Payload.Length];
                LoRaMacCrypto.LoRaMacPayloadDecrypt(Payload, appSkey, DevAddr, true, FCnt, data);
                Payload = data;

                // XTrace.WriteLine("明文数据 Payload " + "FPort " + FPort + " : " + Payload.ToHex());
            }

            NwkSKey = nwkSkey;
            AppSKey = appSkey;
        }

        /// <summary>加密信息</summary>
        /// <param name="key"></param>
        public void Encrypt(Byte[] nwkSkey, Byte[] appSkey)
        {
            // XTrace.WriteLine("DataMessage Encrypt");
            // 没有数据需要解密
            if (((CmdPayload == null) || (CmdPayload.Length == 0)) && ((Payload == null) || (Payload.Length == 0))) return;

            // 没有 Payload ，有 CmdPayload 的时候    FPort 一定等于 0
            // if ((FPort == 0)&& ((Payload == null) || (Payload.Length == 0)))
            if (((Payload == null) || (Payload.Length == 0)))
            {
                if (nwkSkey == null) new NullReferenceException("nwkSkey Error");

                var data = new Byte[CmdPayload.Length];
                LoRaMacCrypto.LoRaMacPayloadEncrypt(CmdPayload, nwkSkey, DevAddr, false, FCnt, data);
                CmdPayload = data;

                // XTrace.WriteLine("密文数据 CmdPayload " + CmdPayload.ToHex());
            }
            else
            {
                if (appSkey == null) new NullReferenceException("appSkey Error");

                var data = new Byte[Payload.Length];
                LoRaMacCrypto.LoRaMacPayloadEncrypt(Payload, appSkey, DevAddr, false, FCnt, data);
                Payload = data;

                // XTrace.WriteLine("密文数据 Payload " + "FPort " + FPort + " : " + Payload.ToHex());
            }
        }

        /// <summary>合并消息, Payload 不能被合并,不判断DevAddr方便行事</summary>
        /// <param name="msg"></param>
        /// <returns>是否合并成功</returns>
        public Boolean Merge(DataMessage msg)
        {
            // 乙方不存在就扔
            if (msg == null) return false;

            if ((Payload != null) && (msg.Payload != null))
            {
                // Payload 不能被合并
                if ((Payload.Length != 0) && (msg.Payload.Length != 0)) return false;
            }

            if ((CmdPayload != null) && (msg.CmdPayload != null))
            {
                // CmdPayload 可以合并 但是长度有限制。
                if (CmdPayload.Length + msg.Payload.Length > 15) return false;
            }

            // 合并 Payload
            if ((Payload == null) || (Payload.Length == 0))
            {
                Payload = msg.Payload;
                FPort = msg.FPort;
            }

            // 合并CmdPayload
            if ((msg.CmdPayload != null) && (msg.CmdPayload.Length != 0))
            {
                var ms = new MemoryStream();
                if (CmdPayload != null) ms.Write(CmdPayload);

                ms.Write(msg.CmdPayload);
                CmdPayload = ms.ToArray();
            }

            // SEQ 跟甲方走
            // 加密秘钥，消息射频信息什么的都跟甲方走

            // ACK判断乙方即可
            if (msg.ACK) ACK = true;
            // 消息类型判断乙方即可
            if (msg.MTpye == MType_e.ConfirmedDataDown) MTpye = msg.MTpye;

            return true;
        }

        ///// <summary>合并消息, Payload 不能被合并</summary>
        ///// <param name="msg"></param>
        ///// <returns>是否合并成功</returns>
        //public Boolean Merge(LoraData msg)
        //{
        //    // 乙方不存在就扔
        //    if (msg == null) return false;

        //    if ((Payload != null) && (msg.Payload != null))
        //    {
        //        // Payload 不能被合并
        //        if ((Payload.Length != 0) && (msg.Payload.Length != 0)) return false;
        //    }

        //    // 合并 Payload
        //    if ((Payload == null) || (Payload.Length == 0))
        //    {
        //        Payload = msg.Payload;
        //        FPort = msg.Port;
        //    }

        //    // ACK判断乙方即可
        //    if (msg.NeedAck) ACK = true;

        //    return true;
        //}

        /// <summary>输出基本消息信息</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var sb = new StringBuilder();
            sb.Append(MTpye);
            // sb.Append(" Addr " + DevAddr.GetBytes().ToHex());
            var port = String.Format("{0:X}", FPort);
            sb.Append(" " + DevAddr.GetBytes().ToHex());
            sb.Append(" Port " + port);
            sb.Append(" FCtrl " + FCtrl.ToString("X2"));
            sb.Append(" Seq " + FCnt.ToString("X4"));

            // sb.Append(" MIC " + MIC.ToString("X8"));
            // if (Payload != null) sb.Append(" Payload " + Payload.ToHex());
            // if (CmdPayload != null) sb.Append(" CmdPayload " + CmdPayload.ToHex());

            return sb.ToString();
        }

        #endregion

        /// <summary>创建回复数据</summary>
        /// <returns></returns>
        public DataMessage CreatReply()
        {
            var msg = new DataMessage
            {
                FCnt = FCnt,
                DevAddr = DevAddr,
                Major = Major,

                RadioPkt = RadioPkt,

                NwkSKey = NwkSKey,
                AppSKey = AppSKey,

                // 默认使用不需要ack的数据包
                MTpye = MType_e.UnconfirmedDataDown
            };

            if (MTpye == MType_e.ConfirmedDataUp)
            {
                msg.ACK = true;
            }

            return msg;
        }

        /// <summary>创建回复数据</summary>
        /// <returns></returns>
        public DataMessage Clone()
        {
            var msg = new DataMessage
            {
                DevAddr = DevAddr,
                MHDR = MHDR,
                FCtrl = FCtrl,
                FCnt = FCnt,
                FPort = FPort,
                MIC = MIC,

                RadioPkt = RadioPkt,
                Valid = Valid,

                NwkSKey = NwkSKey,
                AppSKey = AppSKey,

                MTpye = MTpye
            };

            if (Payload != null) msg.Payload = (Byte[])Payload.Clone();
            if (CmdPayload != null) msg.CmdPayload = (Byte[])CmdPayload.Clone();

            return msg;
        }
    }
}
