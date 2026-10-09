using CAN.Hardware.Attributes;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Parameters;

// Kvaser CAN 调用示例：通过 厂商 -> 设备类型 -> 协议 枚举由工厂创建实例
IHardware? kvaser = FactoryInstance.GetInstance(HardwareManufacturer.Kvaser, KvaserDeviceType.Generic, CommProtocol.CAN);
if (kvaser == null)
{
    Console.WriteLine("未找到匹配的硬件实现。");
    return;
}

// 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
var config = (KvaserCANConfig)kvaser.Config;
config.CanDeviceChannel = 0;                              // 通道号
config.CanBitrate = KvaserCanBitrate.canBITRATE_500K;     // 波特率 500K

// 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
kvaser.eventHandler += OnFrame;

try
{
    if (!kvaser.Open())
    {
        Console.WriteLine("Open failed.");
        return;
    }
    Console.WriteLine("Kvaser CAN opened.");

    // 发送一帧标准帧
    var frame = new CANFrame
    {
        ID = 0x123,
        DLC = 8,
        Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
    };
    bool sent = kvaser.Send(frame);
    Console.WriteLine(sent ? "Frame sent." : "Send failed.");
    Console.ReadKey();
}
catch (Exception ex)
{
    // 未连接硬件/驱动时 Open 会抛出异常
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    kvaser.Close();
    Console.WriteLine("Closed.");
}

static void OnFrame(object? sender, FrameBase frame)
{
    if (frame is CANFrame can)
    {
        string dir = can.Dir == 0 ? "TX" : "RX";
        Console.WriteLine($"{dir} ID=0x{can.ID:X3} DLC={can.DLC} Data={Convert.ToHexString(can.Data)} Flags={can.Flags}");
    }
}
