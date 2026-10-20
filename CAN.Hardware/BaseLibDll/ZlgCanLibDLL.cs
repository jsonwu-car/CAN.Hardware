using CAN.Hardware.Comm;
using System;
using System.Runtime.InteropServices;
namespace CAN.Hardware.BaseLibDll
{
    internal static class ZLGCanLibDLL
    {
        const string DLL_NAME = ".\\HwDependency\\Zlg\\USBCAN\\Win\\x64\\zlgcan.dll";

        // 共享设备句柄管理：同一物理设备只需打开一次，多通道复用
        private static IntPtr _sharedDeviceHandle = IntPtr.Zero;
        private static int _deviceRefCount = 0;
        private static readonly object _deviceLock = new object();

        #region 函数
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr ZCAN_OpenDevice(uint device_type, uint device_index, uint reserved);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        private static extern uint ZCAN_CloseDevice(IntPtr device_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr ZCAN_InitCAN(IntPtr device_handle, uint can_index, IntPtr pInitConfig);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetValue(IntPtr device_handle, string path, string value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ClearBuffer(IntPtr device_handle);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetValue(IntPtr device_handle, string path, IntPtr value);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr ZCAN_GetValue(IntPtr device_handle, string path);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ResetCAN(IntPtr chn_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_StartCAN(IntPtr chn_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetIProperty(IntPtr device_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_Transmit(IntPtr channel_handle, IntPtr pTransmit, uint len);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_TransmitFD(IntPtr channel_handle, IntPtr pTransmit, uint len);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_TransmitData(IntPtr device_handle, IntPtr pTransmit, uint len);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_Receive(IntPtr channel_handle, IntPtr pReceive, uint len, int wait_time = -1);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ReceiveFD(IntPtr channel_handle, IntPtr pReceive, uint len, int wait_time = -1);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ReceiveData(IntPtr device_handle, IntPtr pReceive, uint len, int wait_time);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_GetReceiveNum(IntPtr channel_handle, byte type);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr ZCAN_InitLIN(IntPtr device_handle, uint lin_index, IntPtr pLINInitConfig);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_StartLIN(IntPtr channel_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ResetLIN(IntPtr channel_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_TransmitLIN(IntPtr channel_handle, IntPtr pSend, uint Len);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_GetLINReceiveNum(IntPtr channel_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_ReceiveLIN(IntPtr channel_handle, IntPtr pReceive, uint Len, int WaitTime);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetLINPublish(IntPtr channel_handle, IntPtr pSend, uint nPublishCount);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_SetLINSubscribe(IntPtr channel_handle, IntPtr pSend, uint nSubscribeCount);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_WakeUpLIN(IntPtr channel_handle);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_UDS_Request(IntPtr device_handle, IntPtr req, IntPtr resp, IntPtr dataBuf, uint dataBufSize);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_UDS_Control(IntPtr device_handle, IntPtr ctrl, IntPtr resp);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_UDS_RequestEX(IntPtr device_handle, IntPtr requestData, IntPtr resp, IntPtr dataBuf, uint dataBufSize);


        [DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
        public static extern uint ZCAN_UDS_ControlEX(IntPtr device_handle, uint dataType, IntPtr ctrl, IntPtr resp);
        #endregion

        #region 结构体
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_CHANNEL_INIT_CONFIG
        {
            public uint can_type;
            public _ZCAN_CHANNEL_INIT_CONFIG config;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _ZCAN_CHANNEL_INIT_CONFIG
        {
            public _ZCAN_CHANNEL_CAN_INIT_CONFIG can;
            public _ZCAN_CHANNEL_CANFD_INIT_CONFIG canfd;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _ZCAN_CHANNEL_CAN_INIT_CONFIG
        {
            public uint acc_code;
            public uint acc_mask;
            public uint reserved;
            public byte filter;
            public byte timing0;
            public byte timing1;
            public byte mode;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _ZCAN_CHANNEL_CANFD_INIT_CONFIG
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


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_Transmit_Data
        {
            public can_frame frame;
            public uint transmit_type;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class can_frame
        {
            public uint can_id;
            public byte can_dlc;            // frame payload length in byte (0 .. CAN_MAX_DLEN)
            public byte __pad;             // padding
            public byte __res0;            // reserved / padding
            public byte __res1;            // reserved / padding
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] data;             // frame data
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class canfd_frame
        {
            public uint can_id;
            public byte len;
            public byte flags;
            public byte __res0;
            public byte __res1;  /* reserved / padding */

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] data;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ZCAN_Receive_Data
        {
            public can_frame frame;
            public UInt64 timestamp;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class ZCAN_ReceiveFD_Data
        {
            public canfd_frame frame;
            public UInt64 timestamp;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_AUTO_TRANSMIT_OBJ    //CANFD定时发送帧结构体
        {
            public ushort enable;           //0-禁用，1-使能  
            public ushort index;            //定时报文索引  
            public uint interval;                  //定时周期
            public ZCAN_Transmit_Data obj;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCANFD_AUTO_TRANSMIT_OBJ    //CANFD定时发送帧结构体
        {
            public ushort enable;           //0-禁用，1-使能  
            public ushort index;            //定时报文索引  
            public uint interval;                  //定时周期
            public ZCAN_TransmitFD_Data obj;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_TransmitFD_Data
        {
            public canfd_frame frame; // 报文数据信息，详见 canfd_frame 结构说明。
            public uint transmit_type;      // 发送方式，0=正常发送，1=单次发送，2=自发自收，3=单次自发自收。
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCANCANFDData
        {
            public UInt64 timeStamp;
            public UInt32 flag;                                     // flag用于设置一些参数，内部结构可以通过以下函数实现设置和取值
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] extraData;                                //未使用  
            public canfd_frame frame;                               //实际报文结构体

            // frameType 帧类型 0-CAN 1-CANFD
            public uint frameType
            {
                get { return (flag & 0x03); }
                set { flag = (uint)((flag & ~0x03) | (value & 0x03)); }
            }

            // txDelay队列发送延时，延时时间存放在 timeStamp 字段；0-不启用延时，1-启用延时，单位1ms，2-启用延时，单位100us
            public uint txDelay
            {
                get { return ((flag >> 2) & 0x03); }
                set { flag = (uint)((flag & ~0x0C) | (value & 0x03) << 2); }
            }

            public uint transmitType
            {
                get { return ((flag >> 4) & 0x0F); }
                set { flag = (uint)((flag & ~0x0F) | (value & 0x0F) << 4); }
            }

            public uint txEchoRequest
            {
                get { return ((flag >> 8) & 0x01); }
                set { flag = (uint)(flag | (value & 0x01) << 8); }
            }

            public uint txEchoed
            {
                get { return ((flag >> 9) & 0x01); }  // bit9
                set { flag = (uint)((flag & ~0x200) | (value & 0x01) << 9); }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PID
        {
            public byte rawVal;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RxData
        {
            public UInt64 timeStamp;
            public byte datalen;
            public byte dir;
            public byte chkSum;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
            public byte[] reserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] data;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCANDataObj
        {
            public byte dataType;                                       // 1-CAN/CANFD数据, 4-LIN数据
            public byte chnl;                                           // 数据通道 
            public UInt16 flag;                                         // 未使用
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] extraData;                                    // 未使用  
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 92)]
            public byte[] data;                                    // 报文结构体
        }


        // LIN
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCANLINData
        {
            public PID pid;                 // 受保护的ID
            public RxData rxData;           // 数据
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public byte[] reserved;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCANLINErrData
        {
            public UInt64 timeStamp;        // 时间戳，单位微秒(us)
            public PID pid;                 // 受保护的ID

            public byte dataLen;            // 数据长度
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] data;             // 数据


            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] errData;          // 错误信息

            public byte dir;                // 传输方向
            public byte chkSum;             // 数据校验，部分设备不支持校验数据的获取
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] reserved;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCANLINEventData
        {
            public UInt64 timeStamp;        // 时间戳，单位微秒(us)
            public byte type;               // 数据长度
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public byte[] reserved;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_LIN_MSG
        {
            public byte chnl;                       // 数据通道
            public byte dataType;                   // 0-LIN，1-ErrLIN
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 46)]
            public byte[] data;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_LIN_INIT_CONFIG
        {
            public byte linMode;            // 0-slave,1-master
            public byte chkSumMode;         // 1-经典校验，2-增强校验 3-自动(对应eZLINChkSumMode的模式)
            public byte maxLength;          // 最大数据长度，8~64
            public byte reserved;
            public uint libBaud;            // 波特率，取值1000~20000

        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_LIN_PUBLISH_CFG
        {
            public byte ID;                                         // 受保护的ID（ID取值范围为0-63）
            public byte datelen;                                    // 范围1~8
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]    // 数据段内容
            public byte[] data;
            public byte chkSumMode;                                 // 校验方式：0-默认，启动时配置  1-经典校验  2-增强校验(对应eZLINChkSumMode的模式)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] reserved;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZCAN_LIN_SUBSCIBE_CFG
        {
            public byte ID;                                         // 受保护的ID（ID取值范围为0-63）
            public byte datelen;                                    // dataLen范围为1-8 当为255（0xff）则表示设备自动识别报文长度
            public byte chkSumMode;                                 // 校验方式：0-默认，启动时配置  1-经典校验  2-增强校验(对应eZLINChkSumMode的模式)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] reserved;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SESSION_PARAM
        {
            public uint timeout;            // 响应超时时间(ms)。因PC定时器误差，建议设置不小于200ms
            public uint enhanced_timeout;   // 收到消极响应错误码为0x78后的超时时间(ms)。因PC定时器误差，建议设置不小于200ms
            public byte threeInOne;         //三合一，把下面三个变量写进这一个变量

            // threeInOne 包含以下三个变量
            // BYTE check_any_negative_response : 1;  // 接收到非本次请求服务的消极响应时是否需要判定为响应错误
            // BYTE wait_if_suppress_response   : 1;  // 抑制响应时是否需要等待消极响应，等待时长为响应超时时间
            // BYTE flag                        : 6;  // 保留
            public byte check_any_negative_response
            {
                get { return (byte)(threeInOne & 0x0001); }
                set { threeInOne = (byte)((threeInOne & ~0x0001) | (value & 0x0001)); }
            }
            public byte wait_if_suppress_response
            {
                get { return (byte)((threeInOne & 0x0002) >> 1); }
                set { threeInOne = (byte)((threeInOne & ~0x0002) | (value & 0x0002)); }
            }
            public byte flag
            {
                get { return (byte)((threeInOne & 0x00FC) >> 2); }
                set { threeInOne = (byte)((threeInOne & ~0x00FC) | (value & 0x00FC)); }
            }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
            public byte[] reserved0;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TRANS_PARAM
        {
            public byte version;            // 传输协议版本，VERSION_0，VERSION_1
            public byte max_data_len;       // 单帧最大数据长度，can:8，canfd:64
            public byte local_st_min;       // 本程序发送流控时用，连续帧之间的最小间隔，0x00-0x7F(0ms~127ms)，0xF1-0xF9(100us~900us)
            public byte block_size;         // 流控帧的块大小
            public byte fill_byte;          // 无效字节的填充数据
            public byte ext_frame;          // 0:标准帧 1:扩展帧
            public byte is_modify_ecu_st_min;   // 是否忽略ECU 返回流控的STmin，强制使用本程序设置的remote_st_min 参数代替
            public byte remote_st_min;          // 发送多帧时用, is_modify_ecu_st_min = 1 时有效，0x00 - 0x7F(0ms~127ms), 0xF1 - 0xF9(100us~900us)
            public uint fc_timeout;             // 接收流控超时时间(ms)，如发送首帧后需要等待回应流控帧
            public byte fill_mode;              // 数据长度填充模式 0-就近填充 1-不填充 2-最大填充
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] reserved0;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TRANS_PARAM_LIN
        {
            public byte fill_byte;      // 无效字节的填充数据
            public byte st_min;         // 从节点准备接收诊断请求的下一帧或传输诊断响应的下一帧所需的最小时间
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] reserved0;
        };

        #endregion


        #region 公用方法

        public static uint MakeCanId(uint id, int eff, bool rtr, bool err)
        {
            uint ueff = (uint)(!!(Convert.ToBoolean(eff)) ? 1 : 0);// 0 标准帧，1 扩展帧
            uint urtr = (uint)(!!(Convert.ToBoolean(rtr)) ? 1 : 0);// 0 数据帧，1 远程帧
            uint uerr = (uint)(!!(Convert.ToBoolean(err)) ? 1 : 0); // 0 CAN帧，1 错误帧（目前只能设置为 0）
            return id | ueff << 31 | urtr << 30 | uerr << 29;
        }
        public static uint BuildCanBitrate(ZlgCanBitrate canBitRate)
        {
            switch (canBitRate)
            {
                case ZlgCanBitrate._50K: return 50000;
                case ZlgCanBitrate._100K: return 100000;
                case ZlgCanBitrate._125K: return 125000;
                case ZlgCanBitrate._250K: return 250000;
                case ZlgCanBitrate._500K: return 500000;
                case ZlgCanBitrate._1M: return 1000000;
                default: return 500000;
            }
        }
        public static uint BuildCanFdBitrate(ZlgCanFDBitrate cANFDBitrate)
        {
            switch (cANFDBitrate)
            {
                case ZlgCanFDBitrate._500K_80P: return 500000;
                case ZlgCanFDBitrate._1M_80P: return 1000000;
                case ZlgCanFDBitrate._2M_60P:
                case ZlgCanFDBitrate._2M_80P:
                    return 2000000;
                case ZlgCanFDBitrate._4M_80P: return 4000000;
                case ZlgCanFDBitrate._8M_60P:
                case ZlgCanFDBitrate._8M_70P:
                case ZlgCanFDBitrate._8M_80P:
                    return 8000000;
                default: return 2000000;
            }
        }

        /// <summary>
        /// 打开设备（同一物理设备共享句柄，多通道复用）
        /// </summary>
        public static IntPtr OpenDevice(uint device_type, uint device_index, uint reserved)
        {
            lock (_deviceLock)
            {
                if (_sharedDeviceHandle != IntPtr.Zero)
                {
                    _deviceRefCount++;
                    return _sharedDeviceHandle;
                }

                IntPtr handle = ZCAN_OpenDevice(device_type, device_index, reserved);
                if ((int)handle != 0)
                {
                    _sharedDeviceHandle = handle;
                    _deviceRefCount = 1;
                }
                return handle;
            }
        }

        /// <summary>
        /// 关闭设备（引用计数归零时才真正关闭）
        /// </summary>
        public static void CloseDevice()
        {
            lock (_deviceLock)
            {
                if (_deviceRefCount > 0)
                {
                    _deviceRefCount--;
                    if (_deviceRefCount == 0 && _sharedDeviceHandle != IntPtr.Zero)
                    {
                        ZCAN_CloseDevice(_sharedDeviceHandle);
                        _sharedDeviceHandle = IntPtr.Zero;
                    }
                }
            }
        }
        #endregion
    }
}
