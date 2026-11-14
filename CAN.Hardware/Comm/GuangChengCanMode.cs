namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 广成科技经典 CAN 工作模式(INIT_CONFIG.Mode)。
    /// </summary>
    public enum GuangChengCanMode
    {
        /// <summary>正常模式(正常节点,参与总线应答)</summary>
        Normal = 0,

        /// <summary>只听模式(只接收,不发送,不应答 ACK)</summary>
        ListenOnly = 1,

        /// <summary>自发自收模式(自动将发送帧回环到接收)</summary>
        SelfTest = 2,
    }

    /// <summary>
    /// 广成科技经典 CAN 发送类型(CAN_OBJ.SendType)。
    /// </summary>
    public enum GuangChengCanSendType
    {
        /// <summary>正常发送(失败自动重发)</summary>
        Normal = 0,

        /// <summary>单次发送(不自动重发)</summary>
        SingleShot = 1,

        /// <summary>自发自收(用于自测试)</summary>
        SelfReceive = 2,

        /// <summary>单次自发自收</summary>
        SingleShotSelfReceive = 3,
    }
}
