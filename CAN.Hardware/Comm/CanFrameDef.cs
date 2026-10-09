namespace CAN.Hardware.Comm
{
    internal static class CanFrameDef
    {
        public const uint CAN_STD_MAX_ID_FLAG = 0x7ff;
        public const uint CAN_EFF_FLAG = 0x80000000;
        public const uint CAN_ID_FLAG = 0x1FFFFFFFU;
        public const uint CAN_RTR_FLAG = 0x40000000U; /* remote transmission request */
        public const uint CAN_ERR_FLAG = 0x20000000U; /* error message frame */
    }
}
