using HardwareBench.Core.Models;
using HardwareBench.Core.Serialization;

namespace HardwareBench.Tests.Models;

public class HardwareReportJsonTests
{
    [Fact]
    public void RoundTrip_PreservesAllSections()
    {
        var report = new HardwareReport
        {
            CapturedAtUtc = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero),
            Os = new OsInfo("Microsoft Windows 11 Pro", "10.0.26100", "26100", "64-bit"),
            Cpu = new CpuInfo("Intel Core i7-13700K", "BFEBFBFF000B067A", 16, 24, 5400, 36864),
            Motherboard = new MotherboardInfo("ASUSTeK", "ROG STRIX Z790-E", "MB-123456789"),
            MemoryModules =
            {
                new MemoryModule("BANK 0", "ChannelA-DIMM1", 34359738368, 5600,
                    " Kingston", "KF556C36BBE-16", "12345678", "SODIMM")
            },
            Gpus = { new GpuInfo("NVIDIA GeForce RTX 4070", "NVIDIA", 12247367680, "32.0.15.6094", null) },
            Storage =
            {
                new StorageDevice(@"\\.\PhysicalDrive0", "Samsung SSD 990 PRO 2TB",
                    "S6Z1NJ0R123456", "1B2QJXD7", "Nvme", true, 41, 2048408248320)
            },
            Monitors =
            {
                new MonitorInfo(@"DISPLAY\DEL40FF\5&2b3c4d5&0&0004", "DEL", 0x40FF,
                    "U2415F", "CN0123456789", 2016, 12)
            },
            Peripherals = { new PeripheralInfo("Mouse", "Logitech G Pro Wireless", @"USB\VID_046D&PID_C088\12345678") },
            Errors = { new DetectionError("test.detector", "boom") }
        };

        var json = ReportJson.Serialize(report);
        var back = ReportJson.Deserialize(json);

        Assert.Equal(report.Cpu!.Name, back.Cpu!.Name);
        Assert.Equal(report.Cpu.LogicalProcessors, back.Cpu.LogicalProcessors);
        Assert.Equal(report.MemoryModules[0].CapacityBytes, back.MemoryModules[0].CapacityBytes);
        Assert.Equal(report.Storage[0].TemperatureC, back.Storage[0].TemperatureC);
        Assert.Equal(report.Monitors[0].ProductCode, back.Monitors[0].ProductCode);
        Assert.Equal(report.Peripherals[0].Name, back.Peripherals[0].Name);
        Assert.Equal(report.Errors[0].DetectorId, back.Errors[0].DetectorId);
        Assert.Equal(report.CapturedAtUtc, back.CapturedAtUtc);
    }
}