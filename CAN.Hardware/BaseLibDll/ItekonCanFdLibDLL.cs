using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CAN.Hardware.Comm;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// 爱泰科技(Itekon) CANFD 的 P/Invoke 层。
    /// 对应 iTekCANFD.dll(iTek_ API),设备相关库 USBCANFD.dll / NetCANFD.dll
    /// 由 iTekCANFD.dll 从自身目录加载。
    /// </summary>
    internal static class ItekonCanFdLibDLL
    {
        private const string DLL_NAME = @".\HwDependency\Itekon\CANFD\Win\x64\iTekCANFD.dll";

        /// <summary>发送帧 cantype 标志:0x40 表示发送方向</summary>
        public const byte TX_FLAG = 0x40;

        // 设备句柄按 (device_type, device_index) 共享,多通道复用一个设备句柄
        private sealed class DeviceEntry
        {
            public IntPtr Handle;
            public int RefCount;
        }

        private static readonly object _deviceLock = new object();
        private static readonly Dictionary<(ushort DeviceType, ushort DeviceIndex), DeviceEntry> _devices = new();

        #region 函数
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr iTek_OpenDevice(ushort deviceType, ushort deviceIndex, uint reserved);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern byte iTek_CloseDevice(IntPtr deviceHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr iTek_InitCAN(IntPtr deviceHandle, byte channel, IntPtr config);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint iTek_GetReceiveNum(IntPtr channelHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern void iTek_ClearBuffer(IntPtr channelHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern byte iTek_StartCAN(IntPtr channelHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern byte iTek_ResetCAN(IntPtr channelHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint iTek_Transmit(IntPtr channelHandle, IntPtr data, uint len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint iTek_Receive(IntPtr channelHandle, IntPtr pReceive, uint len, int waitTime);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern uint iTek_Set_Value(IntPtr deviceHandle, [In] byte[] cmd, ref IP_Info value);
        #endregion

        #region 结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct FilterData
        {
            public byte frameType;      //0=标准帧,1=扩展帧
            public byte filterType;     //0=范围ID,1=明确ID,2=掩码ID
            public uint ID1;            //ID 寄存器1
            public uint ID2;            //ID 寄存器2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FilterDataStandard
        {
            public int num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public FilterData[] filterDataStandard;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FilterDataExtend
        {
            public int num;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public FilterData[] filterDataExtend;
        }

        /// <summary>
        /// CAN(FD) 通道初始化参数。标准帧/扩展帧过滤器组至少需有一组有效,
        /// 否则控制器会拒绝接收所有报文。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ItekCanFdChannelInitConfig
        {
            public byte can_type;           //0=CAN,1=CANFD,2=CANFD 加速
            public byte CANFDStandard;      //0=ISO,1=非 ISO
            public byte CANFDSpeedup;       //暂不使用
            public uint abit_timing;        //仲裁段波特率寄存器
            public uint dbit_timing;        //数据段波特率寄存器
            public byte workMode;           //0=正常,1=只听
            public uint res;                //保留
            public FilterDataExtend Extend;
            public FilterDataStandard Standard;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct canfd_frame
        {
            public uint can_id;             //bit31 保留,bit30 EFF,bit29 RTR,低 29 位 ID
            public byte len;                //数据字节数 0-8,12,16,20,24,32,48,64
            public byte flags;              //0=正常数据帧,1=错误帧
            public byte sendType;           //发送方式
            public byte cantype;            //0=CAN,2=CANFD,3=CANFD 加速
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ItekCanFdReceiveData
        {
            public canfd_frame frame;
            public ulong timestamp;         //us
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ItekCanFdTransmitData
        {
            public canfd_frame frame;
            public ushort send_type;        //0=正常发送,1=自发自收
        }

        /// <summary>
        /// 网络型设备 IP 配置(iTek_Set_Value cmd[0]=0x04)。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct IP_Info
        {
            public byte tcp_mod;            //0=UDP,1=TCP Server,2=TCP Client(官方示例;手册文字写 1/2/3)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public byte[] local_ip;
            public ushort local_port;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public byte[] dest_ip;
            public ushort dest_port;
        }
        #endregion

        #region 公用方法
        /// <summary>
        /// 创建带完整过滤器数组的初始化配置(避免 P/Invoke 时数组为 null)。
        /// </summary>
        public static ItekCanFdChannelInitConfig CreateInitConfig()
        {
            return new ItekCanFdChannelInitConfig
            {
                Extend = new FilterDataExtend { filterDataExtend = new FilterData[64] },
                Standard = new FilterDataStandard { filterDataStandard = new FilterData[128] },
            };
        }

        /// <summary>
        /// 按 iTek 的 ID 位定义构造 can_id:bit30=扩展帧,bit29=远程帧,低 29 位为 ID。
        /// </summary>
        public static uint MakeCanId(uint id, bool isExtended, bool isRtr)
        {
            uint raw = id & CanFrameDef.CAN_ID_FLAG;
            if (isExtended) raw |= 0x40000000U;
            if (isRtr) raw |= 0x20000000U;
            return raw;
        }

        /// <summary>
        /// 仲裁段波特率枚举 -> 寄存器值。
        /// </summary>
        public static uint BuildArbitrationTiming(ItekonCanFdArbitrationBitrate bitrate)
        {
            switch (bitrate)
            {
                case ItekonCanFdArbitrationBitrate._5K: return 0x01F31302;
                case ItekonCanFdArbitrationBitrate._10K: return 0x00F91302;
                case ItekonCanFdArbitrationBitrate._20K: return 0x007C1302;
                case ItekonCanFdArbitrationBitrate._40K: return 0x00630A02;
                case ItekonCanFdArbitrationBitrate._50K: return 0x00311302;
                case ItekonCanFdArbitrationBitrate._80K: return 0x001D1204;
                case ItekonCanFdArbitrationBitrate._100K: return 0x00181302;
                case ItekonCanFdArbitrationBitrate._125K: return 0x00131302;
                case ItekonCanFdArbitrationBitrate._200K: return 0x00130A02;
                case ItekonCanFdArbitrationBitrate._250K: return 0x00091302;
                case ItekonCanFdArbitrationBitrate._400K: return 0x00090A02;
                case ItekonCanFdArbitrationBitrate._500K: return 0x00070A02;
                case ItekonCanFdArbitrationBitrate._800K: return 0x00021006;
                case ItekonCanFdArbitrationBitrate._1M: return 0x00040702;
                default: return 0x00070A02;
            }
        }

        /// <summary>
        /// 数据段波特率枚举 -> 寄存器值。
        /// </summary>
        public static uint BuildDataTiming(ItekonCanFdDataBitrate bitrate)
        {
            switch (bitrate)
            {
                case ItekonCanFdDataBitrate._100K: return 0x001D0E30;
                case ItekonCanFdDataBitrate._125K: return 0x001F0A20;
                case ItekonCanFdDataBitrate._200K: return 0x00130A20;
                case ItekonCanFdDataBitrate._250K: return 0x000F0A20;
                case ItekonCanFdDataBitrate._400K: return 0x00090A20;
                case ItekonCanFdDataBitrate._500K: return 0x00070A20;
                case ItekonCanFdDataBitrate._800K: return 0x000E0200;
                case ItekonCanFdDataBitrate._1M: return 0x00020D40;
                case ItekonCanFdDataBitrate._2M: return 0x00010930;
                case ItekonCanFdDataBitrate._3M: return 0x00000D40;
                case ItekonCanFdDataBitrate._4M: return 0x00000930;
                case ItekonCanFdDataBitrate._5M: return 0x00000720;
                case ItekonCanFdDataBitrate._6M: return 0x00000520;
                case ItekonCanFdDataBitrate._8M: return 0x10000A20;
                case ItekonCanFdDataBitrate._10M: return 0x10020100;
                case ItekonCanFdDataBitrate._12M: return 0x10010200;
                default: return 0x00010930;
            }
        }

        /// <summary>
        /// 配置网络型设备的 IP/端口信息(iTek_Set_Value cmd[0]=0x04)。
        /// 返回值:1=成功,0=失败。
        /// </summary>
        public static uint SetIpInfo(IntPtr deviceHandle, ItekonCanFdNetMode netMode,
            string localIp, ushort localPort, string destIp, ushort destPort)
        {
            IP_Info info = new IP_Info
            {
                tcp_mod = (byte)netMode,
                local_ip = new byte[25],
                dest_ip = new byte[25],
            };

            //与官方示例一致:TCP Client 只填目标地址,TCP Server 只填本机地址,UDP 两者都填
            if (netMode != ItekonCanFdNetMode.TcpClient)
            {
                info.local_port = localPort;
                CopyIp(info.local_ip, localIp);
            }
            if (netMode != ItekonCanFdNetMode.TcpServer)
            {
                info.dest_port = destPort;
                CopyIp(info.dest_ip, destIp);
            }

            byte[] cmd = new byte[2];
            cmd[0] = 0x04;
            return iTek_Set_Value(deviceHandle, cmd, ref info);
        }

        private static void CopyIp(byte[] dest, string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return;

            byte[] bytes = Encoding.ASCII.GetBytes(ip.Trim());
            int length = Math.Min(bytes.Length, dest.Length - 1);
            Array.Copy(bytes, dest, length);
        }

        /// <summary>
        /// 是否为网络型设备(NETCANFD/NETCAN/WifiCANFD/WIFICAN 系列)。
        /// 用于决定 Open 时是否需要先下发 IP 配置；官方 C#/Qt 示例中收发仍使用
        /// iTek_InitCAN 返回的通道句柄（手册 4.4～4.7 写 NETCANFD 用设备句柄，存在文字冲突）。
        /// </summary>
        public static bool IsNetDevice(ushort deviceType)
        {
            switch (deviceType)
            {
                case (ushort)ItekonCanFdDeviceModel.NETCANFD_L100:
                case (ushort)ItekonCanFdDeviceModel.NETCANFD_L200:
                case (ushort)ItekonCanFdDeviceModel.NETCAN_1:
                case (ushort)ItekonCanFdDeviceModel.NETCAN_2:
                case (ushort)ItekonCanFdDeviceModel.WIFICANFD_1:
                case (ushort)ItekonCanFdDeviceModel.WIFICANFD_2:
                case (ushort)ItekonCanFdDeviceModel.WIFICAN_1:
                case (ushort)ItekonCanFdDeviceModel.WIFICAN_2:
                case (ushort)ItekonCanFdDeviceModel.NETCANFD_L400:
                case (ushort)ItekonCanFdDeviceModel.NETCANFD_L800:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 打开设备(同一物理设备共享句柄,多通道复用)。
        /// </summary>
        public static IntPtr OpenDevice(ushort deviceType, ushort deviceIndex)
        {
            lock (_deviceLock)
            {
                var key = (deviceType, deviceIndex);
                if (_devices.TryGetValue(key, out DeviceEntry? entry))
                {
                    entry.RefCount++;
                    return entry.Handle;
                }

                IntPtr handle = iTek_OpenDevice(deviceType, deviceIndex, 0);
                // 手册约定失败返回 INVALID_HANDLE_VALUE,示例中按 0 判断,这里两者都视为失败
                if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return IntPtr.Zero;

                _devices[key] = new DeviceEntry { Handle = handle, RefCount = 1 };
                return handle;
            }
        }

        /// <summary>
        /// 关闭设备(引用计数归零时才真正关闭)。
        /// </summary>
        public static void CloseDevice(ushort deviceType, ushort deviceIndex)
        {
            lock (_deviceLock)
            {
                var key = (deviceType, deviceIndex);
                if (!_devices.TryGetValue(key, out DeviceEntry? entry)) return;

                entry.RefCount--;
                if (entry.RefCount > 0) return;

                try { iTek_CloseDevice(entry.Handle); }
                catch { }
                _devices.Remove(key);
            }
        }
        #endregion
    }
}
