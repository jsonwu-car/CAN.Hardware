using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// 图莫斯(Toomoss) USB2XXX 系列适配器的 P/Invoke 层。
    /// 同一 USB2XXX.dll 同时提供经典 CAN(CAN_*)与 CANFD(CANFD_*)两套接口。
    /// </summary>
    internal static class ToomossLibDLL
    {
        private const string DLL_NAME = @".\HwDependency\Toomoss\Generic\Win\x64\USB2XXX.dll";

        public const int CAN_SUCCESS = 0;
        public const int CANFD_SUCCESS = 0;

        public const byte CAN_MSG_REMOTE_RTR = 0x01;
        public const byte CAN_MSG_REMOTE_TX = 0x80;
        public const byte CAN_MODE_RESISTOR = 0x80;

        public const uint CANFD_IDE_FLAG = 0x80000000;
        public const uint CANFD_RTR_FLAG = 0x40000000;
        public const uint CANFD_ID_MASK = 0x1FFFFFFF;

        public const byte CANFD_FLAG_BRS = 0x01;
        public const byte CANFD_FLAG_ESI = 0x02;
        public const byte CANFD_FLAG_FDF = 0x04;
        public const byte CANFD_FLAG_TX = 0x80;

        //USB_ScanDevice 一次最多扫描的设备数量
        private const int MaxDevices = 64;

        private static readonly object _deviceLock = new object();
        private static readonly int[] _deviceHandles = new int[MaxDevices];
        private static int _deviceCount = 0;
        private static readonly Dictionary<int, int> _deviceRefCounts = new();

        #region 函数
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int USB_ScanDevice([Out] int[] pDevHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern byte USB_OpenDevice(int DevHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern byte USB_CloseDevice(int DevHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern byte USB_RetryConnect(int DevHandle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_GetCANSpeedArg(int DevHandle, ref CAN_INIT_CONFIG pCanConfig, uint SpeedBps);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_Init(int DevHandle, byte CANIndex, ref CAN_INIT_CONFIG pCanConfig);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_Filter_Init(int DevHandle, byte CANIndex, ref CAN_FILTER_CONFIG pFilterConfig);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_StartGetMsg(int DevHandle, byte CANIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_StopGetMsg(int DevHandle, byte CANIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_SendMsg(int DevHandle, byte CANIndex, CAN_MSG[] pCanSendMsg, uint SendMsgNum);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_GetMsgWithSize(int DevHandle, byte CANIndex, IntPtr pCanGetMsg, int BufferSize);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_ClearMsg(int DevHandle, byte CANIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CAN_Stop(int DevHandle, byte CANIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_GetCANSpeedArg(int DevHandle, ref CANFD_INIT_CONFIG pCanConfig, uint SpeedBpsNBT, uint SpeedBpsDBT);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_Init(int DevHandle, byte CANIndex, ref CANFD_INIT_CONFIG pCanConfig);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_StartGetMsg(int DevHandle, byte CANIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_StopGetMsg(int DevHandle, byte CANIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_SendMsg(int DevHandle, byte CANIndex, IntPtr pCanSendMsg, uint SendMsgNum);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_GetMsg(int DevHandle, byte CANIndex, IntPtr pCanGetMsg, int BufferSize);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_ClearMsg(int DevHandle, byte CANIndex);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_SetFilter(int DevHandle, byte CANIndex, ref CANFD_FILTER_CONFIG pCanFilter, byte Len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CANFD_Stop(int DevHandle, byte CANIndex);
        #endregion

        #region 结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct CAN_MSG
        {
            public uint ID;             //报文ID(裸ID)
            public uint TimeStamp;      //时间戳低32位,单位100us
            public byte RemoteFlag;     //bit0=RTR,bit6..5=通道号,bit7=发送帧标志
            public byte ExternFlag;     //是否扩展帧
            public byte DataLen;        //数据长度(<=8)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data;
            public byte TimeStampHigh;  //时间戳高位,单位100us
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CAN_INIT_CONFIG
        {
            public uint CAN_BRP;        //取值范围1~1024
            public byte CAN_SJW;        //取值范围1~4
            public byte CAN_BS1;        //取值范围1~16
            public byte CAN_BS2;        //取值范围1~8
            public byte CAN_Mode;       //0正常/1环回/2静默/3静默环回,bit7=内部120Ω
            public byte CAN_ABOM;       //自动离线管理
            public byte CAN_NART;       //0使能重传,1禁止重传
            public byte CAN_RFLM;       //0覆盖旧报文,1丢弃新报文
            public byte CAN_TXFP;       //0 ID优先级,1请求顺序优先级
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CAN_FILTER_CONFIG
        {
            public byte Enable;
            public byte FilterIndex;    //0~13
            public byte FilterMode;     //0屏蔽位模式,1标识符列表模式
            public byte ExtFrame;       //1=扩展帧,0=标准帧
            public uint ID_Std_Ext;
            public uint ID_IDE;
            public uint ID_RTR;
            public uint MASK_Std_Ext;   //对应位为1则需要比较
            public uint MASK_IDE;
            public uint MASK_RTR;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CANFD_MSG
        {
            public uint ID;             //bit31=IDE,bit30=RTR,bit28..0=ID
            public byte DLC;            //实际字节数0~64
            public byte Flags;          //bit0=BRS,bit1=ESI,bit2=FDF,bit6..5=通道,bit7=发送帧标志
            public byte __Res0;
            public byte TimeStampHigh;  //时间戳高位,单位10us
            public uint TimeStamp;      //时间戳低32位,单位10us
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CANFD_INIT_CONFIG
        {
            public byte Mode;           //0正常,1自发自收
            public byte ISOCRCEnable;   //0禁止ISO CRC,1使能
            public byte RetrySend;      //0禁止重发,1无限制重发
            public byte ResEnable;      //0不接入内部120Ω,1接入
            public byte NBT_BRP;
            public byte NBT_SEG1;
            public byte NBT_SEG2;
            public byte NBT_SJW;
            public byte DBT_BRP;
            public byte DBT_SEG1;
            public byte DBT_SEG2;
            public byte DBT_SJW;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] __Res0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CANFD_FILTER_CONFIG
        {
            public byte Enable;
            public byte Index;          //0~31
            public byte __Res0;
            public byte __Res1;
            public uint ID_Accept;      //bit28..0=ID,bit31=IDE
            public uint ID_Mask;        //对应位为1则需要比较
        }
        #endregion

        #region 设备管理
        /// <summary>
        /// 扫描并打开设备(同一物理设备的多通道/多 Wrapper 共享一次 Open)。
        /// </summary>
        public static bool OpenDevice(uint deviceIndex, out int devHandle)
        {
            lock (_deviceLock)
            {
                //没有已打开设备时重新扫描,便于设备热插拔后重新连接
                if (_deviceRefCounts.Count == 0)
                {
                    int count = USB_ScanDevice(_deviceHandles);
                    if (count <= 0)
                    {
                        _deviceCount = 0;
                        devHandle = 0;
                        return false;
                    }
                    _deviceCount = Math.Min(count, MaxDevices);
                }

                int index = (int)deviceIndex;
                if (index < 0 || index >= _deviceCount)
                {
                    devHandle = 0;
                    return false;
                }

                int handle = _deviceHandles[index];
                if (_deviceRefCounts.TryGetValue(handle, out int refCount))
                {
                    _deviceRefCounts[handle] = refCount + 1;
                    devHandle = handle;
                    return true;
                }

                //USB_OpenDevice 返回非 0 表示成功
                if (USB_OpenDevice(handle) == 0)
                {
                    devHandle = 0;
                    return false;
                }

                _deviceRefCounts[handle] = 1;
                devHandle = handle;
                return true;
            }
        }

        /// <summary>
        /// 关闭设备(引用计数归零时才真正 USB_CloseDevice)。
        /// </summary>
        public static void CloseDevice(int devHandle)
        {
            lock (_deviceLock)
            {
                if (!_deviceRefCounts.TryGetValue(devHandle, out int refCount)) return;

                if (refCount > 1)
                {
                    _deviceRefCounts[devHandle] = refCount - 1;
                    return;
                }

                try { USB_CloseDevice(devHandle); }
                catch { }
                _deviceRefCounts.Remove(devHandle);
                if (_deviceRefCounts.Count == 0) _deviceCount = 0;
            }
        }
        #endregion
    }
}
