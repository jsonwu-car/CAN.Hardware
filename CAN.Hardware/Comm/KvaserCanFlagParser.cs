using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Kvaser CAN/CANFD 标志位解析器,产出 <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class KvaserCanFlagParser
    {
        public static CanFrameFlags Parse(uint rawFlag)
        {
            var f = (KvaserMessageFlags)rawFlag;
            return new CanFrameFlags
            {
                RawFlags = rawFlag,

                IsRTR = f.Has(KvaserMessageFlags.Rtr),
                IsStandardFrame = f.Has(KvaserMessageFlags.Std),
                IsExtendedFrame = f.Has(KvaserMessageFlags.Ext),
                IsWakeup = f.Has(KvaserMessageFlags.Wakeup),
                IsNerr = f.Has(KvaserMessageFlags.Nerr),

                IsCanFd = f.Has(KvaserMessageFlags.Fdf),
                HasBrs = f.Has(KvaserMessageFlags.Brs),
                HasEsi = f.Has(KvaserMessageFlags.Esi),

                IsTxAck = f.Has(KvaserMessageFlags.TxAck),
                IsTxRequest = f.Has(KvaserMessageFlags.TxRq),
                IsLocalEcho = f.Has(KvaserMessageFlags.LocalTxAck),
                IsSingleShot = f.Has(KvaserMessageFlags.SingleShot),
                IsTxFailed = f.Has(KvaserMessageFlags.TxNack),
                IsArbitrationLost = f.Has(KvaserMessageFlags.Abl),
                IsDelayMessage = f.Has(KvaserMessageFlags.DelayMsg),

                IsErrorFrame = f.Has(KvaserMessageFlags.ErrorFrame),
                HasHwOverrun = f.Has(KvaserMessageFlags.HwOverrun),
                HasSwOverrun = f.Has(KvaserMessageFlags.SwOverrun),
                HasOverrun = (f & KvaserMessageFlags.Overrun) != 0,
                HasStuffError = f.Has(KvaserMessageFlags.Stuff),
                HasFormError = f.Has(KvaserMessageFlags.Form),
                HasCrcError = f.Has(KvaserMessageFlags.Crc),
                HasBitError = (f & KvaserMessageFlags.BitError) != 0,
                HasBusError = (f & KvaserMessageFlags.BusError) != 0,
            };
        }

        private static bool Has(this KvaserMessageFlags value, KvaserMessageFlags flag)
        {
            return (value & flag) == flag;
        }
    }
}
