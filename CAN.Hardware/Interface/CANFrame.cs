namespace CAN.Hardware.Interface
{
    public class CANFrame : FrameBase
    {
        public int DLC { get; set; }
        public int RTR { get; set; }
        public int Flag { get; set; }
        public ulong Timestamp { get; set; }
        public int Dir { get; set; }
        public bool FDF { get; set; }
        public bool BRS { get; set; }
    }
}
