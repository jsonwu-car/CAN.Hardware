using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// 广成科技 USBCANFD 接口库 P/Invoke 层(ECANFDVCI64.dll)。
    /// 设备类型固定为 USBCANFD=6。
    /// 返回值约定与经典 CAN 不同: 状态型函数 0=STATUS_OK,非 0 为错误码。
    /// </summary>
    internal static class GuangChengCanFdLibDLL
    {
        private const string DLL_NAME = @".\HwDependency\GuangCheng\CANFD\Win\x64\ECANFDVCI64.dll";

        public const uint USBCANFD = 6;
        public const uint STATUS_OK = 0;

        // USBCANFD 以 (DeviceType, DeviceIndex) 标识设备,没有设备句柄;
        // 同一物理设备的多个通道需要共享一次 Open/Close,这里用引用计数管理。
        private static readonly object _deviceLock = new object();
        private static readonly Dictionary<(uint DeviceType, uint DeviceIndex), int> _deviceRefCounts = new();

        #region 函数
        [DllImport(DLL_NAME, EntryPoint = "OpenDeviceFD", CallingConvention = CallingConvention.StdCall)]
        private static extern uint NativeOpenDeviceFD(uint DeviceType, uint DeviceInd);

        [DllImport(DLL_NAME, EntryPoint = "CloseDeviceFD", CallingConvention = CallingConvention.StdCall)]
        private static extern uint NativeCloseDeviceFD(uint DeviceType, uint DeviceInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint InitCANFD(uint DeviceType, uint DeviceInd, byte CANInd, ref INIT_CONFIG pInitConfig);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint StartCANFD(uint DeviceType, uint DeviceInd, byte CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint StopCANFD(uint DeviceType, uint DeviceInd, byte CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ResetCANFD(uint DeviceType, uint DeviceInd, byte CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint TransmitFD(uint DeviceType, uint DeviceInd, byte CANInd, ref CANFD_OBJ pCanfdMQ, uint Len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ReceiveFD(uint DeviceType, uint DeviceInd, byte CANInd, IntPtr pCanfdMQ, ref uint Len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint Receive_buffer_thread(uint DeviceType, uint DeviceInd, uint WaitTime);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint GetErrFrame(uint DeviceType, uint DeviceInd, byte CANInd, IntPtr pCanfdErrbuffer, ref uint Len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint GetCanfdBusStatus(uint DeviceType, uint DeviceInd, IntPtr pCanfdStatus);
        #endregion

        #region 结构体
        /// <summary>
        /// CANFD 时间戳(TIMESTAMP_TYPE),共 8 字节,使用绝对时间(月内的日/时/分/秒/毫秒/微秒)。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TIMESTAMP_TYPE
        {
            public byte mday;
            public byte hour;
            public byte minute;
            public byte second;
            public ushort millisecond;
            public ushort microsecond;
        }

        /// <summary>
        /// CAN/CANFD 信息帧(CANFD_OBJ),共 80 字节。
        /// CanORCanfdType 为按位打包的字节,见 <see cref="BuildFrameType"/>。
        /// DataLen 为实际有效字节数(0~64),不是 DLC 编码。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CANFD_OBJ
        {
            public byte CanORCanfdType;
            public byte DataLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Reserved;
            public uint ID;
            public TIMESTAMP_TYPE TimeStamp;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] Data;
        }

        /// <summary>
        /// CANFD 初始化参数(INIT_CONFIG)。头文件注释写 78 字节,
        /// 但 MSVC 默认对齐下实际为 80 字节,工程顺序布局与原生一致。
        /// NominalBitRate/DataBitRate 由驱动根据 *Select 字段计算并回填,
        /// 不能用于自定义波特率;调用方保持 0。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct INIT_CONFIG
        {
            public byte CanReceMode;
            public byte CanSendMode;
            public uint NominalBitRate;
            public uint DataBitRate;
            public byte FilterUsedBits;
            public byte StdOrExdBits;
            public byte NominalBitRateSelect;
            public byte DataBitRateSelect;
            public uint StandardORExtendedfilter1;
            public uint StandardORExtendedfilter1Mask;
            public uint StandardORExtendedfilter2;
            public uint StandardORExtendedfilter2Mask;
            public uint StandardORExtendedfilter3;
            public uint StandardORExtendedfilter3Mask;
            public uint StandardORExtendedfilter4;
            public uint StandardORExtendedfilter4Mask;
            public uint StandardORExtendedfilter5;
            public uint StandardORExtendedfilter5Mask;
            public uint StandardORExtendedfilter6;
            public uint StandardORExtendedfilter6Mask;
            public uint StandardORExtendedfilter7;
            public uint StandardORExtendedfilter7Mask;
            public uint StandardORExtendedfilter8;
            public uint StandardORExtendedfilter8Mask;
        }
        #endregion

        #region 公用方法
        /// <summary>
        /// 组装 CANFD_OBJ.CanORCanfdType 打包字节。
        /// bit0=CAN/CANFD,bit1=STD/EXT,bit2=DATA/RTR,bit3=数据波特率切换(BRS)。
        /// </summary>
        public static byte BuildFrameType(bool isFd, bool isExtended, bool isRtr, bool brs)
        {
            return (byte)((isFd ? 0x01 : 0x00)
                        | (isExtended ? 0x02 : 0x00)
                        | (isRtr ? 0x04 : 0x00)
                        | (brs ? 0x08 : 0x00));
        }

        /// <summary>
        /// 打开设备(同一物理设备的多个通道共享一次 Open,引用计数管理)。
        /// </summary>
        public static bool OpenDeviceFD(uint deviceType, uint deviceIndex)
        {
            lock (_deviceLock)
            {
                var key = (deviceType, deviceIndex);
                if (_deviceRefCounts.TryGetValue(key, out int refCount) && refCount > 0)
                {
                    _deviceRefCounts[key] = refCount + 1;
                    return true;
                }

                if (NativeOpenDeviceFD(deviceType, deviceIndex) != STATUS_OK) return false;
                _deviceRefCounts[key] = 1;
                return true;
            }
        }

        /// <summary>
        /// 关闭设备(引用计数归零时才真正关闭)。
        /// </summary>
        public static void CloseDeviceFD(uint deviceType, uint deviceIndex)
        {
            lock (_deviceLock)
            {
                var key = (deviceType, deviceIndex);
                if (!_deviceRefCounts.TryGetValue(key, out int refCount)) return;

                if (refCount > 1)
                {
                    _deviceRefCounts[key] = refCount - 1;
                    return;
                }

                try { NativeCloseDeviceFD(deviceType, deviceIndex); }
                catch { }
                _deviceRefCounts.Remove(key);
            }
        }
        #endregion
    }
}
