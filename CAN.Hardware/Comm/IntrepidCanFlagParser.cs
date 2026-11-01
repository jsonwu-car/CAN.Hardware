using CAN.Hardware.Interface;
using static CAN.Hardware.BaseLibDll.IntrepidIcsneoLibDLL;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Intrepid CAN/CANFD 标志位解析器,将 icsSpyMessage 的 StatusBitField/2/3
    /// 解析为统一的 <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class IntrepidCanFlagParser
    {
        public static CanFrameFlags Parse(uint statusBitField, uint statusBitField2, uint statusBitField3)
        {
            var f = (StatusBits)statusBitField;
            var f2 = (StatusBits2)statusBitField2;
            var f3 = (StatusBits3)statusBitField3;

            bool hasCrc = f.Has(StatusBits.CRC_ERROR) || f.Has(StatusBits.CHECKSUM_ERROR);
            bool busOff = f.Has(StatusBits.CAN_BUS_OFF);
            bool errorPassive = f.Has(StatusBits.CAN_ERROR_PASSIVE);
            bool errorWarning = f.Has(StatusBits.CAN_ERROR_WARNING);

            return new CanFrameFlags
            {
                RawFlags = statusBitField,

                IsRTR = f.Has(StatusBits.REMOTE_FRAME),
                IsStandardFrame = !f.Has(StatusBits.XTD_FRAME),
                IsExtendedFrame = f.Has(StatusBits.XTD_FRAME),

                IsCanFd = f3.Has(StatusBits3.CANFD_FDF),
                HasBrs = f3.Has(StatusBits3.CANFD_BRS),
                HasEsi = f3.Has(StatusBits3.CANFD_ESI),

                IsTxAck = f.Has(StatusBits.TX_MSG),

                IsErrorFrame = f.Has(StatusBits.GLOBAL_ERR) || f2.Has(StatusBits2.ERROR_FRAME) || busOff,
                HasSwOverrun = f.Has(StatusBits.COMM_IN_OVERFLOW),
                HasOverrun = f.Has(StatusBits.COMM_IN_OVERFLOW),
                HasCrcError = hasCrc,
                HasBusError = hasCrc || busOff || errorPassive || errorWarning,
            };
        }

        private static bool Has(this StatusBits value, StatusBits flag)
        {
            return (value & flag) == flag;
        }

        private static bool Has(this StatusBits2 value, StatusBits2 flag)
        {
            return (value & flag) == flag;
        }

        private static bool Has(this StatusBits3 value, StatusBits3 flag)
        {
            return (value & flag) == flag;
        }
    }
}
