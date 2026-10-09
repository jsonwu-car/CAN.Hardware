
namespace CAN.Hardware.Comm
{
    internal static class CanFrameHelper
    {
        public static bool IsEFF(long id)
        {
            if ((id & CanFrameDef.CAN_EFF_FLAG)!=0) return true;
            var newId = GetId(id);
            return newId > CanFrameDef.CAN_STD_MAX_ID_FLAG ? true : false;
        }

        public static bool IsRTR(long id)
        {
            return (id & CanFrameDef.CAN_RTR_FLAG) != 0;
        }

        public static bool IsERR(long id)
        {
            return (id & CanFrameDef.CAN_ERR_FLAG) != 0;
        }

        public static uint GetId(long id)
        {
            return (uint)id & CanFrameDef.CAN_ID_FLAG;
        }
    }
}
