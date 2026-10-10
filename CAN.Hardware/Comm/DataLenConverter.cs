namespace CAN.Hardware.Comm
{
    //CAN/CANFD 数据长度校验与DLC编码换算
    public static class DataLenConverter
    {
        public const int MaxCanDataLength = 8;
        public const int MaxCanFdDataLength = 64;

        //CANFD 数据段允许的离散长度
        private static readonly int[] CanFdDataLengths = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 12, 16, 20, 24, 32, 48, 64 };

        //经典CAN：0-8 任意长度
        public static bool IsValidCanLength(int length) => length >= 0 && length <= MaxCanDataLength;

        //CANFD：仅允许 0-8,12,16,20,24,32,48,64
        public static bool IsValidCanFdLength(int length) => Array.IndexOf(CanFdDataLengths, length) >= 0;

        //实际数据长度 -> DLC编码(0-15)，非法长度抛异常
        public static int LengthToDlc(int length)
        {
            if (length <= MaxCanDataLength) return length < 0 ? 0 : length;
            int index = Array.IndexOf(CanFdDataLengths, length);
            if (index < 0)
                throw new ArgumentException($"Invalid CANFD data length({length}), valid lengths: 0-8,12,16,20,24,32,48,64.");
            return index;
        }

        //DLC编码(0-15) -> 实际数据长度，越界钳制到0/64
        public static int DlcToLength(int dlc)
        {
            if (dlc < 0) return 0;
            if (dlc >= CanFdDataLengths.Length) return MaxCanFdDataLength;
            return CanFdDataLengths[dlc];
        }
    }
}
