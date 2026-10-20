
namespace CAN.Hardware.Comm
{
    /// <summary>
    /// ZLG 设备型号,枚举值即 ZCAN_OpenDevice 的 device_type 编号
    /// </summary>
    public enum ZlgDeviceModel : uint
    {
        //ZCAN_USBCAN1 = 3,
        USBCAN_II = 4,
        //ZCAN_USBCAN_E_U = 20,
        USBCAN_2E_U = 21,
        //ZCAN_PCIECANFD_100U = 38,
        //ZCAN_PCIECANFD_200U = 39,
        USBCANFD_200U = 41,
        //ZCAN_USBCANFD_100U = 42,
        //ZCAN_USBCANFD_MINI = 43,
        //ZCAN_PCIECANFD_400U = 61,
        //ZCAN_PCIECANFD_200U_EX = 62,
    }
}
