using System;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Kvaser canRead() 返回的 flag 位定义。
    /// 布局:Bit 0-7 canMSG_xxx,Bit 8-15 canMSGERR_xxx,
    /// Bit 16-23 canFDMSG_xxx,Bit 24-31 扩展标志。
    /// </summary>
    [Flags]
    internal enum KvaserMessageFlags : uint
    {
        None = 0,

        // ---- canMSG_xxx 基本消息属性 ----
        Rtr = 0x0001,
        Std = 0x0002,
        Ext = 0x0004,
        Wakeup = 0x0008,
        Nerr = 0x0010,
        ErrorFrame = 0x0020,
        TxAck = 0x0040,
        TxRq = 0x0080,
        DelayMsg = 0x0100,

        // ---- canMSG_xxx 扩展(高 24-31 位) ----
        SingleShot = 0x01000000,
        TxNack = 0x02000000,
        Abl = 0x04000000,
        LocalTxAck = 0x10000000,

        // ---- canMSGERR_xxx 错误标志 ----
        HwOverrun = 0x0200,
        SwOverrun = 0x0400,
        Stuff = 0x0800,
        Form = 0x1000,
        Crc = 0x2000,
        Bit0 = 0x4000,
        Bit1 = 0x8000,

        // ---- canFDMSG_xxx CAN FD 扩展 ----
        Fdf = 0x010000,
        Brs = 0x020000,
        Esi = 0x040000,

        // ---- 组合掩码 ----
        MsgMask = 0x00FF,
        ErrMask = 0xFF00,
        FdMask = 0xFF0000,
        Overrun = HwOverrun | SwOverrun,
        BitError = Bit0 | Bit1,
        BusError = Stuff | Form | Crc | Bit0 | Bit1,
    }
}
