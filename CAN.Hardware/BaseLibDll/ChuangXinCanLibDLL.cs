using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CAN.Hardware.Comm;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// 创芯科技经典 CAN 分析仪(USBCAN-2A/2C、CANalyst-II 系列)的 P/Invoke 层。
    /// 对应 ControlCAN.dll(VCI_ API),与周立功旧版 ControlCAN 接口兼容。
    /// </summary>
    internal static class ChuangXinCanLibDLL
    {
        private const string DLL_NAME = @".\HwDependency\ChuangXin\CAN\Win\x64\ControlCAN.dll";

        // VCI 以 (DeviceType, DeviceIndex) 标识设备,没有设备句柄;
        // 同一物理设备的多个通道需要共享一次 Open/Close,这里用引用计数管理。
        private static readonly object _deviceLock = new object();
        private static readonly Dictionary<(uint DeviceType, uint DeviceIndex), int> _deviceRefCounts = new();

        #region 函数
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern uint VCI_OpenDevice(uint DeviceType, uint DeviceInd, uint Reserved);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern uint VCI_CloseDevice(uint DeviceType, uint DeviceInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_InitCAN(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_INIT_CONFIG pInitConfig);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_GetReceiveNum(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_ClearBuffer(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_StartCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_ResetCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_Transmit(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pSend, uint Len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_Receive(uint DeviceType, uint DeviceInd, uint CANInd, IntPtr pReceive, uint Len, int WaitTime);
        #endregion

        #region 结构体
        /// <summary>
        /// CAN 信息帧(VCI_CAN_OBJ)。ID 不含标志位,扩展/远程帧由 ExternFlag/RemoteFlag 表示。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct VCI_CAN_OBJ
        {
            public uint ID;
            public uint TimeStamp;          //时间标识,单位 0.1ms
            public byte TimeFlag;           //是否使用时间标识
            public byte SendType;           //发送标志(保留)
            public byte RemoteFlag;         //是否远程帧
            public byte ExternFlag;         //是否扩展帧
            public byte DataLen;            //数据长度
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;
        }

        /// <summary>
        /// 初始化 CAN 的参数(VCI_INIT_CONFIG)。
        /// Filter:0/1=接收所有帧,2=标准帧滤波,3=扩展帧滤波。
        /// Mode:0=正常,1=只听,2=自测。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct VCI_INIT_CONFIG
        {
            public uint AccCode;
            public uint AccMask;
            public uint Reserved;
            public byte Filter;
            public byte Timing0;
            public byte Timing1;
            public byte Mode;
        }
        #endregion

        #region 公用方法
        /// <summary>
        /// 波特率枚举 -> ControlCAN 的 Timing0/Timing1 寄存器值。
        /// </summary>
        public static void BuildTiming(ChuangXinCanBitrate bitrate, out byte timing0, out byte timing1)
        {
            switch (bitrate)
            {
                case ChuangXinCanBitrate._50K: timing0 = 0x09; timing1 = 0x1C; break;
                case ChuangXinCanBitrate._100K: timing0 = 0x04; timing1 = 0x1C; break;
                case ChuangXinCanBitrate._125K: timing0 = 0x03; timing1 = 0x1C; break;
                case ChuangXinCanBitrate._250K: timing0 = 0x01; timing1 = 0x1C; break;
                case ChuangXinCanBitrate._500K: timing0 = 0x00; timing1 = 0x1C; break;
                case ChuangXinCanBitrate._800K: timing0 = 0x00; timing1 = 0x16; break;
                case ChuangXinCanBitrate._1M: timing0 = 0x00; timing1 = 0x14; break;
                default: timing0 = 0x00; timing1 = 0x1C; break;
            }
        }

        /// <summary>
        /// 打开设备(同一物理设备的多个通道共享一次 Open,引用计数管理)。
        /// </summary>
        public static bool OpenDevice(uint deviceType, uint deviceIndex)
        {
            lock (_deviceLock)
            {
                var key = (deviceType, deviceIndex);
                if (_deviceRefCounts.TryGetValue(key, out int refCount) && refCount > 0)
                {
                    _deviceRefCounts[key] = refCount + 1;
                    return true;
                }

                // VCI_OpenDevice 返回 1=STATUS_OK,0=失败
                if (VCI_OpenDevice(deviceType, deviceIndex, 0) != 1) return false;
                _deviceRefCounts[key] = 1;
                return true;
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
                if (!_deviceRefCounts.TryGetValue(key, out int refCount)) return;

                if (refCount > 1)
                {
                    _deviceRefCounts[key] = refCount - 1;
                    return;
                }

                try { VCI_CloseDevice(deviceType, deviceIndex); }
                catch { }
                _deviceRefCounts.Remove(key);
            }
        }
        #endregion
    }
}
