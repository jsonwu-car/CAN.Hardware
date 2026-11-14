namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 广成科技 USBCANFD 接收模式(INIT_CONFIG.CanReceMode)。
    /// </summary>
    public enum GuangChengCanFdReceiveMode
    {
        /// <summary>特定接收模式,使用滤波器 1~8</summary>
        Specified = 0,

        /// <summary>只接收标准帧</summary>
        GlobalStandard = 1,

        /// <summary>只接收扩展帧</summary>
        GlobalExtended = 2,

        /// <summary>标准帧与扩展帧都接收</summary>
        GlobalStandardAndExtended = 3,
    }

    /// <summary>
    /// 广成科技 USBCANFD 发送模式(INIT_CONFIG.CanSendMode)。
    /// </summary>
    public enum GuangChengCanFdSendMode
    {
        /// <summary>主动发送:不使用发送缓冲,必须等上一批数据发完才能再次发送</summary>
        Positive = 0,

        /// <summary>被动发送:使用发送缓冲,缓冲未用完即可继续发送</summary>
        Passive = 1,
    }
}
