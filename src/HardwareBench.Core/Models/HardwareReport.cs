namespace HardwareBench.Core.Models;

public sealed class HardwareReport
{
    public DateTimeOffset CapturedAtUtc { get; set; }
    public OsInfo? Os { get; set; }
    public CpuInfo? Cpu { get; set; }
    public MotherboardInfo? Motherboard { get; set; }
    public List<MemoryModule> MemoryModules { get; set; } = [];
    public List<GpuInfo> Gpus { get; set; } = [];
    public List<StorageDevice> Storage { get; set; } = [];
    public List<MonitorInfo> Monitors { get; set; } = [];
    public List<PeripheralInfo> Peripherals { get; set; } = [];
    public List<DetectionError> Errors { get; set; } = [];
}

public sealed record OsInfo(string Name, string Version, string BuildNumber, string Architecture);