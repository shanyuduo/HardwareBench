using System.Runtime.Versioning;
using Hardware.Info;

namespace HardwareBench.Core.Detection.Wmi;

[SupportedOSPlatform("windows")]
public sealed class HardwareInfoWmiSource : IWmiSource
{
    private readonly HardwareInfo _hw = new();

    public IEnumerable<CPU> GetCpuList() { _hw.RefreshCPUList(); return _hw.CpuList; }
    public IEnumerable<Motherboard> GetMotherboardList() { _hw.RefreshMotherboardList(); return _hw.MotherboardList; }
    public IEnumerable<Memory> GetMemoryList() { _hw.RefreshMemoryList(); return _hw.MemoryList; }
    public IEnumerable<VideoController> GetVideoControllerList() { _hw.RefreshVideoControllerList(); return _hw.VideoControllerList; }
    public IEnumerable<Drive> GetDriveList() { _hw.RefreshDriveList(); return _hw.DriveList; }
    public OS GetOperatingSystem() { _hw.RefreshOperatingSystem(); return _hw.OperatingSystem; }
}