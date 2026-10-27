using System.Runtime.InteropServices;
using System.Text;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// NI-XNET(nixnet.dll) C API 互操作层。
    /// nixnet.dll 随 NI-XNET 驱动安装到系统目录(如 C:\Windows\System32),
    /// 32/64 位进程均可使用,调用约定为 Cdecl。
    /// 帧内存布局(nxFrame_t): 16 字节帧头 + 8 字节对齐的负载区,
    /// 基本单元 24 字节(帧头 + 8 字节负载),负载超过 8 字节时按 8 字节为步进追加负载单元。
    /// </summary>
    internal static class NiXnetLibDLL
    {
        // nixnet.dll 由 NI-XNET 驱动安装到系统目录,使用裸 DLL 名由系统搜索路径解析
        private const string DLL_NAME = "nixnet.dll";

        // nxStatus_t 成功码
        internal const int NX_SUCCESS = 0;

        // 帧布局常量
        internal const int NX_FRAME_HEADER_SIZE = 16;   // nxFrame_t 帧头(时间戳8 + ID4 + Type1 + Flags1 + Info1 + PayloadLength1)
        internal const int NX_MAX_PAYLOAD = 64;         // CAN FD 最大负载

        // 扩展帧标记: XNET 使用 ID 的 bit29(0x20000000),而非常见的 bit31
        internal const uint NX_CAN_EXT_FLAG = 0x20000000;
        internal const uint NX_CAN_ID_MASK = 0x1FFFFFFF;

        // 帧类型(nxFrameType_t, CAN 部分)
        internal const byte NX_FRAME_TYPE_CAN_DATA = 0x00;      // CAN 数据帧(IO Mode=CAN 时收发)
        internal const byte NX_FRAME_TYPE_CAN_REMOTE = 0x01;    // CAN 远程帧
        internal const byte NX_FRAME_TYPE_CAN_BUS_ERROR = 0x02; // CAN 总线错误帧
        internal const byte NX_FRAME_TYPE_CAN20_DATA = 0x08;    // CAN 2.0 数据帧(IO Mode=CAN FD/BRS 时收发经典帧)
        internal const byte NX_FRAME_TYPE_CANFD_DATA = 0x10;    // CAN FD 数据帧(无 BRS)
        internal const byte NX_FRAME_TYPE_CANFDBRS_DATA = 0x18; // CAN FD 数据帧(带 BRS)

        // 帧标志(Flags 字节): bit7 为发送回显(本节点发出后被 nxRead 返回)
        internal const byte NX_FRAME_FLAG_TRANSMIT_ECHO = 0x80;

        // nxCreateSession 的 Mode 参数
        internal const uint NX_MODE_FRAME_IN_STREAM = 6;   // Frame In Stream
        internal const uint NX_MODE_FRAME_OUT_STREAM = 9;  // Frame Out Stream

        // nxStart/nxStop 的 Scope 参数
        internal const uint NX_SCOPE_NORMAL = 0;

        // nxSetProperty 属性 ID(属性号 | 类ID | 类型ID)
        internal const uint NX_PROP_SESSION_INTF_BAUD_RATE64 = 0x09100016;        // Session:Intf:Baud Rate (u64, bps)
        internal const uint NX_PROP_SESSION_INTF_CAN_FD_BAUD_RATE64 = 0x09100027; // Session:Intf:CAN FD Baud Rate (u64, bps)

        // 内存伪数据库: :memory: 对应经典 CAN, :can_fd_brs: 对应 CAN FD + BRS
        internal const string DB_CAN = ":memory:";
        internal const string DB_CAN_FD_BRS = ":can_fd_brs:";

        // 接收缓冲区(64KB, 高负载下一帧 80 字节可容纳 800+ 帧)
        internal const int NX_READ_BUFFER_SIZE = 0x10000;

        // nxReadFrame 单次阻塞超时(秒),保证 Close 时读线程可在 100ms 内退出
        internal const double NX_READ_TIMEOUT = 0.1;

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int nxCreateSession(string databaseName, string clusterName, string list, string interfaceName, uint mode, out uint sessionRef);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int nxClear(uint sessionRef);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int nxStart(uint sessionRef, uint scope);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int nxStop(uint sessionRef, uint scope);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int nxFlush(uint sessionRef);

        [DllImport(DLL_NAME, EntryPoint = "nxSetProperty", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int nxSetProperty64(uint sessionRef, uint propertyId, uint propertySize, ref ulong propertyValue);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int nxReadFrame(uint sessionRef, [Out] byte[] buffer, uint sizeOfBuffer, double timeout, out uint numberOfBytesReturned);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int nxWriteFrame(uint sessionRef, [In] byte[] buffer, uint numberOfBytesForFrames, double timeout);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nxStatusToString(int status, [Out] byte[] statusString, uint sizeOfStatusString);

        /// <summary>
        /// 一帧在流中占用的字节数: 帧头 + max(8, 向上 8 字节对齐的负载区)
        /// </summary>
        internal static int GetFrameSize(int payloadLength)
        {
            return NX_FRAME_HEADER_SIZE + Math.Max(8, (payloadLength + 7) & ~7);
        }

        /// <summary>
        /// 按 nxFrame_t 内存布局手工打包一帧(避免结构体编组,布局完全可控)
        /// </summary>
        internal static byte[] BuildFrameBytes(uint identifier, byte frameType, byte[] data, int payloadLength)
        {
            byte[] buffer = new byte[GetFrameSize(payloadLength)];
            // 偏移 0: Timestamp(u64),发送时由硬件回填,置 0 即可
            BitConverter.GetBytes(identifier).CopyTo(buffer, 8);          // 偏移 8: Identifier(u32)
            buffer[12] = frameType;                                       // 偏移 12: Type
            // 偏移 13: Flags, 偏移 14: Info, 发送时均置 0
            buffer[15] = (byte)payloadLength;                             // 偏移 15: PayloadLength
            Array.Copy(data, 0, buffer, NX_FRAME_HEADER_SIZE, payloadLength);
            return buffer;
        }

        /// <summary>
        /// 将 nxStatus 码转为可读错误描述
        /// </summary>
        internal static string GetStatusString(int status)
        {
            if (status == NX_SUCCESS) return "Success";
            try
            {
                byte[] buffer = new byte[1024];
                nxStatusToString(status, buffer, (uint)buffer.Length);
                int len = Array.IndexOf(buffer, (byte)0);
                if (len < 0) len = buffer.Length;
                return Encoding.ASCII.GetString(buffer, 0, len);
            }
            catch
            {
                return $"0x{status:X8}";
            }
        }
    }
}
