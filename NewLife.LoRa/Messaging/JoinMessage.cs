using System;
using System.IO;
using System.Text;
using NewLife.Log;

namespace NewLife.LoRa.Messaging
{
    // 命令简介
    // MHDR(1 byte)  LoRaMacAppEui(8 byte)  LoRaMacDevEui(8 byte)  LoRaMacDevNonce(2 byte)  MIC(4 byte)
    // MHDR(1 byte)  AppNonce(3 byte)  NetID(3 byte)  DevAddr(4 byte)  DLSettings(1 byte)  RxDelay(1 byte)  CFList(pad16 0/16byte) MIC(4 byte)

    /*
     具体内容： 
     
    首先，一个End Node需要配置：AppEUI和DevEUI；随机值，得到DevNonce。
    将这3个参数，组织成Join Request数据帧，发送给LoRaWAN LoraWan。

    Server接收到Join Request后，分配DevAddr，连同AppNonce和NetID，
    组织成JoinAccept数据帧，回应给EndNode。

    End Node接收Join Accept后，提取DevAddr；
    结合4个参数：AppKey、AppNonce、NetID和DevNonce，使用aes128_encrypt()，生成2个密钥：NekSKey和AppSKey。


            数据包格式  

    设备节点发送的注册信息
        MHDR(1 byte)  LoRaMacAppEui(8 byte)  LoRaMacDevEui(8 byte)  LoRaMacDevNonce(2 byte)  MIC(4 byte)
        其中只有  MIC 是  前面数据计算的之外，其他全部明文。

    服务器回复数据的解包方式
        使用 LoRaMacAppKey （节点本地存储）解密（LoRaMacJoinDecrypt） 除第一字节（MHDR）之外的所有数据。得到 data。
        LoRaMacJoinComputeMic 计算data的 除最后后4节的 数据的mic。 校验数据签名。
        LoRaMacJoinComputeSKeys 函数提取 LoRaMacNwkSKey  LoRaMacAppSKey  此函数不改变数据包内容。
        此时数据包 内容是
        MHDR(1 byte)  AppNonce(3 byte)  NetID(3 byte)  DevAddr(4 byte)  DLSettings(1 byte)  RxDelay(1 byte)  CFList(pad16 0/16byte) MIC(4 byte)

    提取 NetId （3字节） 提取 DevAddr（4字节）  提取 RxDelay(1 byte)  解析 CFList(pad16)
    RxDelay 是接收窗口1 。  OTAA 在此进行处理！！！ 接收窗口2的值为 RxDelay + 1   单位秒。
    DLSettings 是接收窗口的扩频因子。[6,4]3bit是接收窗口1的  [3,0]4bit是接收窗口2的
    CFList(pad16) 是可选项 内容是信道信息
    */

    /// <summary>节点发送的入网请求命令</summary>
    public class JoinRequest
    {
        #region 节点数据包内容
        /// <summary>应用编号8byte</summary>
        public Int64 AppEui { get; set; }

        /// <summary>节点为唯一编号8 byte</summary>
        public Int64 DevEui { get; set; }

        /// <summary>节点生成的随机数，用于消息ID 2byte</summary>
        public UInt16 DevNonce { get; set; }

        #endregion

        #region 本地数据

        /// <summary>对的key  AppKey</summary>
        private Byte[] _Key;

        #endregion

        #region 构造

        public JoinRequest() => MTpye = MType_e.JoinRequest;

        #endregion

        #region 方法
        /// <summary>读取JoinRequest数据包</summary>
        /// <param name="bs"></param>
        public override void Read(Byte[] bs)
        {
            if (bs == null)
            {
                XTrace.WriteLine("数据为空");
                return;
            }
            // 判断数据包长度
            if (bs.Length < 23)
            {
                XTrace.WriteLine("数据长度{0} 不满足要求", bs.Length);
                Valid = false;
                return;
            }

            Buffer = bs;
            XTrace.WriteLine("JoinRequest len {0}:{1}", bs.Length, bs.ToHex());

            var ms = new MemoryStream(bs);
            // MHDR(1 byte)  LoRaMacAppEui(8 byte)  LoRaMacDevEui(8 byte)  LoRaMacDevNonce(2 byte)  MIC(4 byte)
            MHDR = (Byte)ms.ReadByte();
            AppEui = BitConverter.ToInt64(ms.ReadBytes(8), 0);
            DevEui = BitConverter.ToInt64(ms.ReadBytes(8), 0);
            DevNonce = ms.ReadBytes(2).ToUInt16();
            MIC = ms.ReadBytes(4).ToUInt32();

            // XTrace.WriteLine(ToString());
        }

        /// <summary>使用key去校验数据合法性</summary>
        /// <param name="key">AppEui对应的key</param>
        /// <returns>校验结果</returns>
        public Boolean ValidationData(Byte[] key)
        {
            if (!Valid) return false;

            if (key == null)
            {
                XTrace.WriteLine("ValidationData key == null");
                return false;
            }
            if (key.Length != 16)
            {
                XTrace.WriteLine("ValidationData key.len error");
                return false;
            }

            if ((Buffer == null) || (Buffer.Length < 23))
            {
                XTrace.WriteLine("ValidationData Buffer error");
                return false;
            }

            // 提取校验部分数据
            var bs2 = Buffer.ReadBytes(0, Buffer.Length - 4);

            UInt32 mic = 0;
            // 使用 key 去校验数据
            LoRaMacCrypto.LoRaMacJoinComputeMic(bs2, key, ref mic);
            if (MIC != mic)
            {
                XTrace.WriteLine("数据包校验失败，MIC 计算值 {0} 包内值 {1} ", mic.GetBytes().ToHex(), MIC.GetBytes().ToHex());
                Valid = false;
                return false;
            }

            _Key = key;
            Valid = true;
            return true;
        }

        /// <summary>创建回复数据</summary>
        /// <returns></returns>
        public JoinAccept CreatReply()
        {
            var msg = new JoinAccept
            {
                Major = Major,
                RFU = RFU,

                DevNonce = DevNonce,
                AppKey = _Key,
                // 射频信息还需要继续附带
                RadioPkt = RadioPkt
            };

            return msg;
        }

        public override String ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AppEui ");
            sb.Append(AppEui.GetBytes().ToHex());
            sb.Append(" DevEui ");
            sb.Append(DevEui.GetBytes().ToHex());
            sb.Append(" DevNonce ");
            sb.Append(DevNonce.GetBytes().ToHex());
            sb.Append(" MIC ");
            sb.Append(MIC.GetBytes().ToHex());

            return sb.ToString();
        }

        #endregion
    }

    /// <summary>网关回复节点请求命令</summary>
    public class JoinAccept
    {
        #region 服务器回复数据内容

        private Byte[] _AppNonce;
        /// <summary>应用随机数 3byte</summary>
        public Byte[] AppNonce
        {
            get =>
                // if (_AppNonce == null) _AppNonce = new byte[3];
                // if (_AppNonce.Length != 3) _AppNonce = new byte[3];
                _AppNonce;
            set
            {
                if (value.Length != 3)
                {
                    XTrace.WriteLine("AppNonce 必须是3字节");
                    return;
                }
                _AppNonce = value;
            }
        }

        private Byte[] _NetId;
        /// <summary>网络ID  节点只做储存，没有参与具体业务，可以通过命令让节点返回 3byte</summary>
        public Byte[] NetId
        {
            get =>
                // if (_NetId == null) _NetId = new byte[3];
                // if (_NetId.Length != 3) _NetId = new byte[3];
                _NetId;
            set
            {
                if (value.Length != 3)
                {
                    XTrace.WriteLine("NetId 必须是3字节");
                    return;
                }
                _NetId = value;
            }
        }

        /// <summary>设备地址</summary>
        public Int32 DevAddr;

        /// <summary>接收窗口参数1byte</summary>
        private Byte DLSettings;

        /// <summary>接收窗口1 DR偏移 3bit</summary>
        public Int32 Rx1DrOffset
        {
            get => (Byte)((DLSettings >> 4) & 0x07);
            set
            {
                if (value > 7) return;
                DLSettings &= 0x8f;
                DLSettings |= (Byte)(value << 4);
            }
        }

        /// <summary>接收通道2 DR 值</summary>
        public Int32 Rx2DR
        {
            get => (Byte)(DLSettings & 0x0F);
            set
            {
                if (value > 0x0f) return;
                DLSettings &= 0xf0;
                DLSettings |= (Byte)value;
            }
        }

        private Byte _ReceiveDelay1 = 1;

        /// <summary>接收窗口1 单位秒,  接收窗口2 比此值大1</summary>
        public Int32 RxWindow1Delay
        {
            get => _ReceiveDelay1;
            set
            {
                if (value < 1) { XTrace.WriteLine("set RxWindow1Delay 太小 {0}", value); return; }
                if (value > 15) { XTrace.WriteLine("set RxWindow1Delay 太大 {0}", value); return; }
                _ReceiveDelay1 = (Byte)value;
            }
        }

        /// <summary>节点端RegionCN470ApplyCFList函数未实现！</summary>
        public Byte[] CFList { get => null; set { XTrace.WriteLine("CFList 未实现"); return; } }

        #endregion

        #region 服务器需要使用的但不是服务器生成的

        /// <summary>终端生成的随机数，用于数据包标记</summary>
        public UInt16 DevNonce;

        #endregion

        #region 服务器生成的，但是数据包不直接包含的

        /// <summary>网络秘钥 16字节</summary>
        public Byte[] NwkSKey = new Byte[16];

        /// <summary>应用秘钥 16字节</summary>
        public Byte[] AppSKey = new Byte[16];

        /// <summary>AppEui 对应的key 也就是本数据加密时候需要使用的key</summary>
        public Byte[] AppKey = new Byte[16];

        #endregion

        #region 构造
        public JoinAccept() => MTpye = MType_e.JoinAccept;

        #endregion

        #region 方法

        /// <summary>序列化包</summary>
        /// <returns></returns>
        public override Byte[] ToArray()
        {
            if (Buffer == null) Build();

            return Buffer;
        }

        // 创建出消息的具体内容，包含创建出通讯秘钥，加密等。
        public void Build()
        {
            // MHDR(1 byte)  AppNonce(3 byte)  NetID(3 byte)  DevAddr(4 byte)  DLSettings(1 byte)  RxDelay(1 byte)  CFList(pad16 0 / 16byte) MIC(4 byte)
            var ms = new MemoryStream();

            // 回复注册消息
            MTpye = MType_e.JoinAccept;
            ms.Write(MHDR);

            // if (AppNonce == null) { XTrace.WriteLine("AppNonce is null"); return; }
            // if (AppNonce.Length != 3) { XTrace.WriteLine("AppNonce Length error"); return; }
            if (AppNonce == null)
            {
                var rd = new Random();
                var nonce = rd.Next();
                AppNonce = nonce.GetBytes().ReadBytes(0, 3);
            }

            if (NetId == null) { XTrace.WriteLine("NetId is null"); return; }
            if (NetId.Length != 3) { XTrace.WriteLine("NetId Length error"); return; }

            ms.Write(AppNonce);
            ms.Write(NetId);
            ms.Write(DevAddr.GetBytes());
            ms.Write(DLSettings);
            ms.Write(_ReceiveDelay1);

            if (CFList != null)
            {
                if (CFList.Length == 16) ms.Write(CFList);
            }

            LoRaMacCrypto.LoRaMacJoinComputeMic(ms.ToArray(), AppKey, ref MIC);

            // 先写签名
            ms.Write(MIC.GetBytes());
            // 后加密
            // XTrace.WriteLine("明文前 len {0} : {1}", ms.Position, ms.ToArray().ToHex());

            // 构建加密后的数据流
            var ms2 = new MemoryStream();
            // 重新写入数据
            ms2.Write(MHDR);
            // 加密数据
            var endata = Encrypt(ms.ToArray().ReadBytes(1));

            if (endata == null) return;
            ms2.Write(endata);
            // XTrace.WriteLine("加密后 len {0}:{1}", ms2.Position, ms2.ToArray().ToHex());

            // 到这里了  数据包已经OK 了   可以计算出key  以备使用
            GetSKey();

            // 获取完整包
            Buffer = ms2.ToArray();
        }

        /// <summary>加密  此处代码不保险  独立函数</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private Byte[] Encrypt(Byte[] data)
        {
            var rs = new Byte[data.Length];

            if (LoRaMacCrypto.LoRaMacJoinEncrypt(data, AppKey, rs))
            {
                // XTrace.WriteLine("src data:{0}",data.ToHex());
                // XTrace.WriteLine("dst data:{0}", rs.ToHex());
                // XTrace.WriteLine("重复一遍测试");
                // var rs2 = new byte[data.Length];
                // LoRaMacCrypto.LoRaMacJoinDecrypt(rs, AppKey, rs2);
                // XTrace.WriteLine("xxx data:{0}", rs2.ToHex());

                return rs;
            }

            return null;
        }

        /// <summary>计算出AppSKey 和 NwkSKey</summary>
        private void GetSKey()
        {
            var ms = new MemoryStream();
            ms.Write(AppNonce);
            ms.Write(NetId);

            if (!LoRaMacCrypto.LoRaMacJoinComputeSKeys(AppKey, ms.ToArray(), DevNonce, NwkSKey, AppSKey))
            {
                XTrace.WriteLine("参数有问题 请检查");
            }

            XTrace.WriteLine("NwkSKey {0}, AppSKey {1}", NwkSKey.ToHex(), AppSKey.ToHex());
        }

        public override String ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AppNonce ");
            if (AppNonce != null) sb.Append(AppNonce.ToHex());

            sb.Append(" NetId ");
            if (NetId != null) sb.Append(NetId.ToHex());

            sb.Append(" DevAddr ");
            sb.Append(DevAddr.GetBytes().ToHex());

            sb.Append(" DevNonce ");
            sb.Append(DevNonce.GetBytes().ToHex());

            sb.Append(" RxWindow1Delay ");
            sb.Append(_ReceiveDelay1);

            sb.Append(" MIC ");
            sb.Append(MIC.GetBytes().ToHex());

            return sb.ToString();
        }

        #endregion
    }
}
