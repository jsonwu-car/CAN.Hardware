using CAN.Hardware.Attributes;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Parameters;

// 依次演示 Kvaser 经典 CAN 与 CAN FD 的调用
DemoCan();
DemoCanFd();
// PEAK 演示需要实际 PEAK 硬件(PCAN-USB 等)及 PCAN-Basic 驱动,默认不启用
// DemoPeakCan();
// DemoPeakCanFd();

// Kvaser CAN 调用示例：通过 厂商 -> 设备类型 -> 协议 枚举由工厂创建实例
static void DemoCan()
{
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
}

// Kvaser CAN FD 调用示例：与经典 CAN 一致，仅协议枚举、配置类型与帧标志不同
static void DemoCanFd()
{
    IHardware? kvaserFd = FactoryInstance.GetInstance(HardwareManufacturer.Kvaser, KvaserDeviceType.Generic, CommProtocol.CANFD);
    if (kvaserFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（仲裁段 500K，数据段 2M）
    var config = (KvaserCanFDConfig)kvaserFd.Config;
    config.CanFdDeviceChannel = 0;                                            // 通道号
    config.CanFdArbitrationBitrate = KvaserCanBitrate.canBITRATE_500K;        // 仲裁段波特率 500K
    config.CanFdDataBitrate = KvaserCanFdBitrate.canFD_BITRATE_2M_80P;        // 数据段波特率 2M

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    kvaserFd.eventHandler += OnFrame;

    try
    {
        if (!kvaserFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Kvaser CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧（FDF + BRS）
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = kvaserFd.Send(frame);
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
        kvaserFd.Close();
        Console.WriteLine("Closed.");
    }
}

// PEAK CAN 调用示例：与 Kvaser 一致，仅厂商/设备类型枚举、配置类型不同
static void DemoPeakCan()
{
    IHardware? peak = FactoryInstance.GetInstance(HardwareManufacturer.Peak, PeakDeviceType.Generic, CommProtocol.CAN);
    if (peak == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
    var config = (PeakCANConfig)peak.Config;
    config.CanDeviceChannel = 0x51;                           // 通道句柄(PCAN_USBBUS1)
    config.CanBitrate = PeakCanBitrate.pcanBAUD_500K;         // 波特率 500K

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    peak.eventHandler += OnFrame;

    try
    {
        if (!peak.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Peak CAN opened.");

        // 发送一帧标准帧
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = peak.Send(frame);
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
        peak.Close();
        Console.WriteLine("Closed.");
    }
}

// PEAK CAN FD 调用示例：仲裁段 500K，数据段 2M（Clock 80MHz 下默认参数）
static void DemoPeakCanFd()
{
    IHardware? peakFd = FactoryInstance.GetInstance(HardwareManufacturer.Peak, PeakDeviceType.Generic, CommProtocol.CANFD);
    if (peakFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（默认即为 80MHz 时钟下仲裁段 500K、数据段 2M）
    var config = (PeakCanFDConfig)peakFd.Config;
    config.CanFdDeviceChannel = 0x51;                         // 通道句柄(PCAN_USBBUS1)

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    peakFd.eventHandler += OnFrame;

    try
    {
        if (!peakFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Peak CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧（FDF + BRS）
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = peakFd.Send(frame);
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
        peakFd.Close();
        Console.WriteLine("Closed.");
    }
}

static void OnFrame(object? sender, FrameBase frame)
{
    if (frame is CANFrame can)
    {
        string dir = can.Dir == 0 ? "TX" : "RX";
        Console.WriteLine($"{dir} ID=0x{can.ID:X3} DLC={can.DLC} Data={Convert.ToHexString(can.Data)} Flags={can.Flags}");
    }
}
