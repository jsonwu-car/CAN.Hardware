using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CAN.Hardware.Comm;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// 广成科技 USBCAN-I/II 经典 CAN 接口库 P/Invoke 层(ECanVci64.dll)。
    /// 设备类型: USBCAN1=3(单通道)、USBCAN2=4(双通道)。
    /// 返回值约定: 状态型函数 1=STATUS_OK; Transmit 返回实际发送帧数;
    /// Receive 返回实际读取帧数,0xFFFFFFFF 表示读取失败。
    /// </summary>
    internal static class GuangChengCanLibDLL
    {
        private const string DLL_NAME = @".\HwDependency\GuangCheng\CAN\Win\x64\ECanVci64.dll";

        public const uint USBCAN1 = 3;
        public const uint USBCAN2 = 4;
        public const uint STATUS_OK = 1;
        public const uint STATUS_ERR = 0;
        public const uint RECEIVE_FAILED = 0xFFFFFFFF;

        // ECanVci 以 (DeviceType, DeviceIndex) 标识设备,没有设备句柄;
        // 同一物理设备的多个通道需要共享一次 Open/Close,这里用引用计数管理。
        private static readonly object _deviceLock = new object();
        private static readonly Dictionary<(uint DeviceType, uint DeviceIndex), int> _deviceRefCounts = new();

        #region 函数
        [DllImport(DLL_NAME, EntryPoint = "OpenDevice", CallingConvention = CallingConvention.StdCall)]
        private static extern uint NativeOpenDevice(uint DeviceType, uint DeviceInd, uint Reserved);

        [DllImport(DLL_NAME, EntryPoint = "CloseDevice", CallingConvention = CallingConvention.StdCall)]
        private static extern uint NativeCloseDevice(uint DeviceType, uint DeviceInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint InitCAN(uint DeviceType, uint DeviceInd, uint CANInd, ref INIT_CONFIG pInitConfig);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint GetReceiveNum(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ClearBuffer(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint StartCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ResetCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint Transmit(uint DeviceType, uint DeviceInd, uint CANInd, ref CAN_OBJ pSend, uint Len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint Receive(uint DeviceType, uint DeviceInd, uint CANInd, IntPtr pReceive, uint Len, int WaitTime);
        #endregion

        #region 结构体
        /// <summary>
        /// CAN 信息帧(CAN_OBJ),共 24 字节。
        /// ID 不含标志位,扩展帧/远程帧分别由 ExternFlag/RemoteFlag 表示。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CAN_OBJ
        {
            public uint ID;
            public uint TimeStamp;          //时间标识,单位微秒(TimeFlag!=0 时有效)
            public byte TimeFlag;           //是否使用时间标识
            public byte SendType;           //发送帧类型
            public byte RemoteFlag;         //是否远程帧
            public byte ExternFlag;         //是否扩展帧
            public byte DataLen;            //数据长度(0~8)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Reserved;
        }

        /// <summary>
        /// 初始化 CAN 的参数(INIT_CONFIG),共 16 字节。
        /// Timing0/Timing1 为 BTR0/BTR1 寄存器,可自定义非标波特率。
        /// Filter: 0=不使能硬件滤波,1=使能(使用 SJA1000 的 AccCode/AccMask)。
        /// Mode: 0=正常,1=只听,2=自发自收。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct INIT_CONFIG
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
        /// 波特率枚举 -> SJA1000 Timing0(BTR0)/Timing1(BTR1) 寄存器值。
        /// </summary>
        public static void BuildTiming(GuangChengCanBitrate bitrate, out byte timing0, out byte timing1)
        {
            switch (bitrate)
            {
                case GuangChengCanBitrate._5K: timing0 = 0xBF; timing1 = 0xFF; break;
                case GuangChengCanBitrate._10K: timing0 = 0x31; timing1 = 0x1C; break;
                case GuangChengCanBitrate._20K: timing0 = 0x18; timing1 = 0x1C; break;
                case GuangChengCanBitrate._40K: timing0 = 0x87; timing1 = 0xFF; break;
                case GuangChengCanBitrate._50K: timing0 = 0x09; timing1 = 0x1C; break;
                case GuangChengCanBitrate._80K: timing0 = 0x83; timing1 = 0xFF; break;
                case GuangChengCanBitrate._100K: timing0 = 0x04; timing1 = 0x1C; break;
                case GuangChengCanBitrate._125K: timing0 = 0x03; timing1 = 0x1C; break;
                case GuangChengCanBitrate._200K: timing0 = 0x81; timing1 = 0xFA; break;
                case GuangChengCanBitrate._250K: timing0 = 0x01; timing1 = 0x1C; break;
                case GuangChengCanBitrate._400K: timing0 = 0x80; timing1 = 0xFA; break;
                case GuangChengCanBitrate._500K: timing0 = 0x00; timing1 = 0x1C; break;
                case GuangChengCanBitrate._666K: timing0 = 0x80; timing1 = 0xB6; break;
                case GuangChengCanBitrate._800K: timing0 = 0x00; timing1 = 0x16; break;
                case GuangChengCanBitrate._1M: timing0 = 0x00; timing1 = 0x14; break;
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

                if (NativeOpenDevice(deviceType, deviceIndex, 0) != STATUS_OK) return false;
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

                try { NativeCloseDevice(deviceType, deviceIndex); }
                catch { }
                _deviceRefCounts.Remove(key);
            }
        }
        #endregion
    }
}
