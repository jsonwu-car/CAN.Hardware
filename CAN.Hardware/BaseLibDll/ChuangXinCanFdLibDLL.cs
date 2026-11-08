using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CAN.Hardware.Comm;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// 创芯科技 CANFD 分析仪(USBCANFD-200U/100U/MINI)的 P/Invoke 层。
    /// 对应 ControlCANFD.dll(ZCAN_ API),与周立功 zlgcan 接口同源。
    /// </summary>
    internal static class ChuangXinCanFdLibDLL
    {
        private const string DLL_NAME = @".\HwDependency\ChuangXin\CANFD\Win\x64\ControlCANFD.dll";

        /// <summary>通道类型:经典 CAN</summary>
        public const uint TYPE_CAN = 0;

        /// <summary>通道类型:CAN FD</summary>
        public const uint TYPE_CANFD = 1;

        /// <summary>成功状态值(ControlCANFD 与 ZLG 一致:1=成功,0=失败)</summary>
        public const uint STATUS_OK = 1;

        // 设备句柄按 (device_type, device_index) 共享,多通道复用一个句柄
        private sealed class DeviceEntry
        {
            public IntPtr Handle;
            public int RefCount;
        }

        private static readonly object _deviceLock = new object();
        private static readonly Dictionary<(uint DeviceType, uint DeviceIndex), DeviceEntry> _devices = new();

        #region 函数
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ZCAN_OpenDevice(uint device_type, uint device_index, uint reserved);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern uint ZCAN_CloseDevice(IntPtr device_handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr ZCAN_InitCAN(IntPtr device_handle, uint can_index, IntPtr pInitConfig);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_StartCAN(IntPtr channel_handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ResetCAN(IntPtr channel_handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ClearBuffer(IntPtr channel_handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_Transmit(IntPtr channel_handle, IntPtr pTransmit, uint len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_TransmitFD(IntPtr channel_handle, IntPtr pTransmit, uint len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_Receive(IntPtr channel_handle, IntPtr pReceive, uint len, int wait_time);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ReceiveFD(IntPtr channel_handle, IntPtr pReceive, uint len, int wait_time);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetAbitBaud(IntPtr device_handle, uint can_index, uint abitbaud);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetDbitBaud(IntPtr device_handle, uint can_index, uint dbitbaud);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetCANFDStandard(IntPtr device_handle, uint can_index, uint canfd_standard);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetResistanceEnable(IntPtr device_handle, uint can_index, uint enable);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetBaudRateCustom(IntPtr device_handle, uint can_index,
            [MarshalAs(UnmanagedType.LPStr)] string rateCustom);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ClearFilter(IntPtr channel_handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetFilterMode(IntPtr channel_handle, uint mode);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetFilterStartID(IntPtr channel_handle, uint startID);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetFilterEndID(IntPtr channel_handle, uint endID);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_AckFilter(IntPtr channel_handle);
        #endregion

        #region 结构体
        [StructLayout(LayoutKind.Sequential)]
        public struct can_frame
        {
            public uint can_id;             //32 bit MAKE_CAN_ID + EFF/RTR/ERR flags
            public byte can_dlc;            //frame payload length in byte (0 .. CAN_MAX_DLEN)
            public byte __pad;
            public byte __res0;
            public byte __res1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct canfd_frame
        {
            public uint can_id;             //32 bit MAKE_CAN_ID + EFF/RTR/ERR flags
            public byte len;                //frame payload length in byte (0 .. CANFD_MAX_DLEN)
            public byte flags;              //bit0=BRS, bit1=ESI
            public byte __res0;
            public byte __res1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ZCAN_CAN_INIT_CONFIG
        {
            public uint acc_code;
            public uint acc_mask;
            public uint reserved;
            public byte filter;
            public byte timing0;
            public byte timing1;
            public byte mode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ZCAN_CANFD_INIT_CONFIG
        {
            public uint acc_code;
            public uint acc_mask;
            public uint abit_timing;
            public uint dbit_timing;
            public uint brp;
            public byte filter;
            public byte mode;
            public ushort pad;
            public uint reserved;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ZCAN_CHANNEL_INIT_CONFIG
        {
            [FieldOffset(0)] public uint can_type;    //TYPE_CAN / TYPE_CANFD
            [FieldOffset(4)] public ZCAN_CAN_INIT_CONFIG can;
            [FieldOffset(4)] public ZCAN_CANFD_INIT_CONFIG canfd;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ZCAN_Transmit_Data
        {
            public can_frame frame;
            public uint transmit_type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ZCAN_Receive_Data
        {
            public can_frame frame;
            public ulong timestamp;         //us
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ZCAN_TransmitFD_Data
        {
            public canfd_frame frame;
            public uint transmit_type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ZCAN_ReceiveFD_Data
        {
            public canfd_frame frame;
            public ulong timestamp;         //us
        }
        #endregion

        #region 公用方法
        public static uint MakeCanId(uint id, bool eff, bool rtr, bool err)
        {
            uint raw = id & CanFrameDef.CAN_ID_FLAG;
            if (eff) raw |= CanFrameDef.CAN_EFF_FLAG;
            if (rtr) raw |= CanFrameDef.CAN_RTR_FLAG;
            if (err) raw |= CanFrameDef.CAN_ERR_FLAG;
            return raw;
        }

        public static uint BuildCanBitrate(ChuangXinCanBitrate canBitrate)
        {
            switch (canBitrate)
            {
                case ChuangXinCanBitrate._50K: return 50000;
                case ChuangXinCanBitrate._100K: return 100000;
                case ChuangXinCanBitrate._125K: return 125000;
                case ChuangXinCanBitrate._250K: return 250000;
                case ChuangXinCanBitrate._500K: return 500000;
                case ChuangXinCanBitrate._800K: return 800000;
                case ChuangXinCanBitrate._1M: return 1000000;
                default: return 500000;
            }
        }

        public static uint BuildCanFdBitrate(ChuangXinCanFdBitrate canFdBitrate)
        {
            switch (canFdBitrate)
            {
                case ChuangXinCanFdBitrate._100K: return 100000;
                case ChuangXinCanFdBitrate._125K: return 125000;
                case ChuangXinCanFdBitrate._250K: return 250000;
                case ChuangXinCanFdBitrate._500K: return 500000;
                case ChuangXinCanFdBitrate._800K: return 800000;
                case ChuangXinCanFdBitrate._1M: return 1000000;
                case ChuangXinCanFdBitrate._2M: return 2000000;
                case ChuangXinCanFdBitrate._4M: return 4000000;
                case ChuangXinCanFdBitrate._5M: return 5000000;
                default: return 2000000;
            }
        }

        /// <summary>
        /// 打开设备(同一物理设备共享句柄,多通道复用)。
        /// </summary>
        public static IntPtr OpenDevice(uint deviceType, uint deviceIndex)
        {
            lock (_deviceLock)
            {
                var key = (deviceType, deviceIndex);
                if (_devices.TryGetValue(key, out DeviceEntry? entry))
                {
                    entry.RefCount++;
                    return entry.Handle;
                }

                IntPtr handle = ZCAN_OpenDevice(deviceType, deviceIndex, 0);
                if (handle == IntPtr.Zero) return IntPtr.Zero;

                _devices[key] = new DeviceEntry { Handle = handle, RefCount = 1 };
                return handle;
            }
        }

        /// <summary>
        /// 关闭设备(引用计数归零时才真正关闭)。
        /// </summary>
        public static void CloseDevice(uint deviceType, uint deviceIndex)
        {
            lock (_deviceLock)
            {
                var key = (deviceType, deviceIndex);
                if (!_devices.TryGetValue(key, out DeviceEntry? entry)) return;

                entry.RefCount--;
                if (entry.RefCount > 0) return;

                try { ZCAN_CloseDevice(entry.Handle); }
                catch { }
                _devices.Remove(key);
            }
        }
        #endregion
    }
}
