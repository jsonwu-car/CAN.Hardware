using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Parameters;

// 依次演示 Kvaser 经典 CAN 与 CAN FD 的调用
DemoCan();
DemoCanFd();
// PEAK 演示需要实际 PEAK 硬件(PCAN-USB 等)及 PCAN-Basic 驱动,默认不启用
// DemoPeakCan();
// DemoPeakCanFd();
// TOSUN 演示需要实际同星硬件(TC1014 等)及 libTSCAN 驱动,默认不启用
// DemoTosunCan();
// DemoTosunCanFd();
// ZLG 演示需要实际周立功硬件(USBCAN-2E-U/USBCANFD-200U 等)及 zlgcan 驱动,默认不启用
// DemoZlgCan();
// DemoZlgCanFd();
// Vector 演示需要实际 Vector 硬件(VN1630 等)及 Vector XL Driver,默认不启用
// DemoVectorCan();
// DemoVectorCanFd();
// ESD 演示需要实际 ESD 硬件(CAN-USB/400 等)及 ESD NTCAN 驱动,默认不启用
// DemoEsdCan();
// DemoEsdCanFd();
// ETAS 演示需要实际 ETAS 硬件(ES581/ES584/ES592 等)及 ETAS BOA V2 驱动(随 INCA 等工具链安装),默认不启用
// DemoEtasCan();
// DemoEtasCanFd();
// NI 演示需要实际 NI 硬件(USB-8502 等)及 NI-XNET 驱动,默认不启用
// DemoNiCan();
// DemoNiCanFd();
// Intrepid 演示需要实际 Intrepid 硬件(neoVI FIRE/ValueCAN4 等)及 neoVI API(icsneo40.dll)驱动,默认不启用
// DemoIntrepidCan();
// DemoIntrepidCanFd();
// EbEl 演示需要实际 Eberspächer FlexCard 硬件及 FlexCard 驱动(fcbase.dll),默认不启用;注意 FlexCard 只有经典 CAN
// DemoEbElCan();
// 创芯科技经典 CAN 演示需要实际硬件(USBCAN-2A/2C、CANalyst-II 等)及 ControlCAN.dll,默认不启用
// DemoChuangXinCan();
// 创芯科技 CANFD 演示需要实际 USBCANFD 硬件(200U/100U/MINI)及 ControlCANFD.dll,默认不启用
// DemoChuangXinCanFd();

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

// TOSUN CAN 调用示例：与 PEAK 一致，仅厂商/设备类型枚举、配置类型不同
static void DemoTosunCan()
{
    IHardware? tosun = FactoryInstance.GetInstance(HardwareManufacturer.Tosun, TosunDeviceType.Generic, CommProtocol.CAN);
    if (tosun == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
    var config = (ToSunCANConfig)tosun.Config;
    config.ChannelIndex = CHANNEL_INDEX.CHN1;                 // 通道索引(0基)
    config.CanBitrate = 500;                                  // 波特率 500K(Kbps)

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    tosun.eventHandler += OnFrame;

    try
    {
        if (!tosun.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Tosun CAN opened.");

        // 发送一帧标准帧
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = tosun.Send(frame);
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
        tosun.Close();
        Console.WriteLine("Closed.");
    }
}

// TOSUN CAN FD 调用示例：默认仲裁段 500K，数据段 2M(Kbps)
static void DemoTosunCanFd()
{
    IHardware? tosunFd = FactoryInstance.GetInstance(HardwareManufacturer.Tosun, TosunDeviceType.Generic, CommProtocol.CANFD);
    if (tosunFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（默认即为仲裁段 500K、数据段 2M）
    var config = (ToSunCanFDConfig)tosunFd.Config;
    config.ChannelIndex = CHANNEL_INDEX.CHN1;                 // 通道索引(0基)

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    tosunFd.eventHandler += OnFrame;

    try
    {
        if (!tosunFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Tosun CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧（FDF + BRS）
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = tosunFd.Send(frame);
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
        tosunFd.Close();
        Console.WriteLine("Closed.");
    }
}

// ZLG CAN 调用示例：与 PEAK 一致，仅厂商/设备类型枚举、配置类型不同
static void DemoZlgCan()
{
    IHardware? zlg = FactoryInstance.GetInstance(HardwareManufacturer.Zlg, ZlgDeviceType.USBCAN, CommProtocol.CAN);
    if (zlg == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
    var config = (ZlgCANConfig)zlg.Config;
    config.DeviceModel = ZlgDeviceModel.USBCAN_2E_U;          // 设备型号
    config.DeviceIndex = 0;                                   // 设备索引号
    config.DeviceChannel = 0;                                 // 通道号(0基)
    config.CanBitrate = ZlgCanBitrate._500K;                  // 波特率 500K

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    zlg.eventHandler += OnFrame;

    try
    {
        if (!zlg.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("ZLG CAN opened.");

        // 发送一帧标准帧
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = zlg.Send(frame);
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
        zlg.Close();
        Console.WriteLine("Closed.");
    }
}

// ZLG CAN FD 调用示例：默认仲裁段 500K，数据段 2M
static void DemoZlgCanFd()
{
    IHardware? zlgFd = FactoryInstance.GetInstance(HardwareManufacturer.Zlg, ZlgDeviceType.USBCAN, CommProtocol.CANFD);
    if (zlgFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（默认即为 USBCANFD-200U、仲裁段 500K、数据段 2M）
    var config = (ZlgCanFDConfig)zlgFd.Config;
    config.DeviceModel = ZlgDeviceModel.USBCANFD_200U;        // 设备型号
    config.DeviceIndex = 0;                                   // 设备索引号
    config.DeviceChannel = 0;                                 // 通道号(0基)

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    zlgFd.eventHandler += OnFrame;

    try
    {
        if (!zlgFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("ZLG CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧（FDF + BRS）
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = zlgFd.Send(frame);
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
        zlgFd.Close();
        Console.WriteLine("Closed.");
    }
}

// Vector CAN 调用示例：与 PEAK 一致，仅厂商/设备类型枚举、配置类型不同
static void DemoVectorCan()
{
    IHardware? vector = FactoryInstance.GetInstance(HardwareManufacturer.Vector, VectorDeviceType.Generic, CommProtocol.CAN);
    if (vector == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
    var config = (VectorCanConfig)vector.Config;
    config.HardwareType = VectorHwType.VN1630;                // Vector 硬件型号
    config.Channel = new XLchannelInfo { HwChannel = 0 };     // 硬件通道号(0基)
    config.CanBitrate = VectorCanBitrate.canBITRATE_500K;     // 波特率 500K

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    vector.eventHandler += OnFrame;

    try
    {
        if (!vector.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Vector CAN opened.");

        // 发送一帧标准帧
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = vector.Send(frame);
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
        vector.Close();
        Console.WriteLine("Closed.");
    }
}

// Vector CAN FD 调用示例：默认仲裁段 500K，数据段 2M
static void DemoVectorCanFd()
{
    IHardware? vectorFd = FactoryInstance.GetInstance(HardwareManufacturer.Vector, VectorDeviceType.Generic, CommProtocol.CANFD);
    if (vectorFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（默认即为 VN1630、仲裁段 500K、数据段 2M）
    var config = (VectorCanFDConfig)vectorFd.Config;
    config.HardwareType = VectorHwType.VN1630;                // Vector 硬件型号
    config.Channel = new XLchannelInfo { HwChannel = 0 };     // 硬件通道号(0基)
    config.CanFdArbitrationBitrate = VectorCanBitrate.canBITRATE_500K;    // 仲裁段波特率 500K
    config.CanFdDataBitrate = VectorCanFdBitrate.canFD_BITRATE_2M;        // 数据段波特率 2M

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    vectorFd.eventHandler += OnFrame;

    try
    {
        if (!vectorFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Vector CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧（FDF + BRS）
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = vectorFd.Send(frame);
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
        vectorFd.Close();
        Console.WriteLine("Closed.");
    }
}

// ESD CAN 调用示例：与 PEAK 一致，仅厂商/设备类型枚举、配置类型不同
static void DemoEsdCan()
{
    IHardware? esd = FactoryInstance.GetInstance(HardwareManufacturer.Esd, EsdDeviceType.Generic, CommProtocol.CAN);
    if (esd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
    var config = (EsdCANConfig)esd.Config;
    config.Channel = 0;                                     // 通道号(0基,直接传给 canOpen)
    config.CanBitrate = EsdCanBitrate.esdBAUD_500K;         // 波特率 500K,枚举值即 bps
    // 自定义波特率(任意 bps 数值,驱动自动计算位时序):
    // config.EnableCustomBitrate = true;
    // config.CustomBitrate = 250000;                        // 例如 250K

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    esd.eventHandler += OnFrame;

    try
    {
        if (!esd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("ESD CAN opened.");

        // 发送一帧标准帧
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = esd.Send(frame);
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
        esd.Close();
        Console.WriteLine("Closed.");
    }
}

// ESD CAN FD 调用示例：默认仲裁段 500K，数据段 2M
static void DemoEsdCanFd()
{
    IHardware? esdFd = FactoryInstance.GetInstance(HardwareManufacturer.Esd, EsdDeviceType.Generic, CommProtocol.CANFD);
    if (esdFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（默认即为仲裁段 500K、数据段 2M）
    var config = (EsdCanFDConfig)esdFd.Config;
    config.Channel = 0;                                               // 通道号(0基)
    config.CanFdArbitrationBitrate = EsdCanBitrate.esdBAUD_500K;      // 仲裁段波特率 500K
    config.CanFdDataBitrate = EsdCanFdBitrate.esdBAUD_2M;             // 数据段波特率 2M
    // 自定义波特率(任意 bps 数值,驱动自动计算位时序):
    // config.EnableCustomBitrate = true;
    // config.CustomArbitrationBitrate = 500000;                       // 自定义仲裁段 500K
    // config.CustomDataBitrate = 4000000;                             // 自定义数据段 4M

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    esdFd.eventHandler += OnFrame;

    try
    {
        if (!esdFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("ESD CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧（FDF + BRS）
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = esdFd.Send(frame);
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
        esdFd.Close();
        Console.WriteLine("Closed.");
    }
}

// Intrepid CAN 调用示例：与 PEAK 一致，仅厂商/设备类型枚举、配置类型不同
static void DemoIntrepidCan()
{
    IHardware? intrepid = FactoryInstance.GetInstance(HardwareManufacturer.Intrepid, IntrepidDeviceType.Generic, CommProtocol.CAN);
    if (intrepid == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
    var config = (IntrepidCANConfig)intrepid.Config;
    config.Channel = 0;                                          // 通道号(0基,对应设备 HSCAN1)
    config.CanBitrate = IntrepidCanBitrate.icsBAUD_500K;         // 波特率 500K
    // 自定义波特率(任意 bps 数值,驱动自动取最近值):
    // config.EnableCustomBitrate = true;
    // config.CustomBitrate = 250000;                             // 例如 250K

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    intrepid.eventHandler += OnFrame;

    try
    {
        if (!intrepid.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Intrepid CAN opened.");

        // 发送一帧标准帧
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = intrepid.Send(frame);
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
        intrepid.Close();
        Console.WriteLine("Closed.");
    }
}

// Intrepid CAN FD 调用示例：默认仲裁段 500K，数据段 2M
static void DemoIntrepidCanFd()
{
    IHardware? intrepidFd = FactoryInstance.GetInstance(HardwareManufacturer.Intrepid, IntrepidDeviceType.Generic, CommProtocol.CANFD);
    if (intrepidFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（默认即为仲裁段 500K、数据段 2M）
    var config = (IntrepidCanFDConfig)intrepidFd.Config;
    config.Channel = 0;                                                   // 通道号(0基,对应设备 HSCAN1)
    config.CanFdArbitrationBitrate = IntrepidCanBitrate.icsBAUD_500K;     // 仲裁段波特率 500K
    config.CanFdDataBitrate = IntrepidCanFdBitrate.icsFDBAUD_2M;          // 数据段波特率 2M

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    intrepidFd.eventHandler += OnFrame;

    try
    {
        if (!intrepidFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("Intrepid CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧（FDF + BRS）
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = intrepidFd.Send(frame);
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
        intrepidFd.Close();
        Console.WriteLine("Closed.");
    }
}

// EbEl(Eberspächer Electronics FlexCard) CAN 调用示例：仅经典 CAN(FlexCard 无 CAN FD)
static void DemoEbElCan()
{
    IHardware? ebel = FactoryInstance.GetInstance(HardwareManufacturer.EbEl, EbElDeviceType.Generic, CommProtocol.CAN);
    if (ebel == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数（Config 返回 BusConfigBase，转为具体配置类型后设置）
    var config = (EbElCANConfig)ebel.Config;
    config.Channel = 0;                                       // 通道号(0基,对应设备 CC1)
    config.CanBitrate = EbElCanBitrate.fcBAUD_500K;           // 波特率 500K(按位时序表配置)

    // 订阅报文推送（Rx 接收与 Tx 回显都会通过该事件推送）
    ebel.eventHandler += OnFrame;

    try
    {
        if (!ebel.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("EbEl CAN opened.");

        // 发送一帧标准帧
        // 注意: FlexCard 发送是缓冲式(fcbCANTransmit 只给缓冲号+数据),ID 由缓冲配置决定
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = ebel.Send(frame);
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
        ebel.Close();
        Console.WriteLine("Closed.");
    }
}

// 创芯科技经典 CAN 调用示例:USBCAN-2A/2C、CANalyst-II 系列
static void DemoChuangXinCan()
{
    IHardware? chuangXin = FactoryInstance.GetInstance(HardwareManufacturer.ChuangXin, ChuangXinDeviceType.USBCAN, CommProtocol.CAN);
    if (chuangXin == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数(Config 返回 BusConfigBase,转为具体配置类型后设置)
    var config = (ChuangXinCANConfig)chuangXin.Config;
    config.DeviceModel = ChuangXinCanDeviceModel.USBCAN2;        // USBCAN-2A/2C、CANalyst-II 常用值
    config.DeviceIndex = 0;                                    // 设备索引号
    config.DeviceChannel = 0;                                  // 通道号(0基)
    config.CanBitrate = ChuangXinCanBitrate._500K;                // 波特率 500K

    // 订阅报文推送(Rx 接收与 Tx 回显都会通过该事件推送)
    chuangXin.eventHandler += OnFrame;

    try
    {
        if (!chuangXin.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("ChuangXin CAN opened.");

        // 发送一帧标准帧
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 8,
            Data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 },
        };
        bool sent = chuangXin.Send(frame);
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
        chuangXin.Close();
        Console.WriteLine("Closed.");
    }
}

// 创芯科技 CANFD 调用示例:USBCANFD-200U/100U/MINI
static void DemoChuangXinCanFd()
{
    IHardware? chuangXinFd = FactoryInstance.GetInstance(HardwareManufacturer.ChuangXin, ChuangXinDeviceType.USBCANFD, CommProtocol.CANFD);
    if (chuangXinFd == null)
    {
        Console.WriteLine("未找到匹配的硬件实现。");
        return;
    }

    // 配置硬件参数(默认即为 USBCANFD-200U、仲裁段 500K、数据段 2M)
    var config = (ChuangXinCanFDConfig)chuangXinFd.Config;
    config.DeviceModel = ChuangXinCanFdDeviceModel.USBCANFD_200U; // 设备型号
    config.DeviceIndex = 0;                                    // 设备索引号
    config.DeviceChannel = 0;                                  // 通道号(0基)
    config.UseCanFD = true;                                    // 启用 CANFD 模式

    // 订阅报文推送(Rx 接收与 Tx 回显都会通过该事件推送)
    chuangXinFd.eventHandler += OnFrame;

    try
    {
        if (!chuangXinFd.Open())
        {
            Console.WriteLine("Open failed.");
            return;
        }
        Console.WriteLine("ChuangXin CAN FD opened.");

        // 发送一帧 64 字节的 CAN FD 帧(FDF + BRS)
        var frame = new CANFrame
        {
            ID = 0x123,
            DLC = 64,
            Data = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(),
            FDF = true,
            BRS = true,
        };
        bool sent = chuangXinFd.Send(frame);
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
        chuangXinFd.Close();
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
