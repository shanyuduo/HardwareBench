using Hardware.Info;

namespace HardwareBench.Core.Detection.Wmi;

public interface IWmiSource
{
    IEnumerable<CPU> GetCpuList();
    IEnumerable<Motherboard> GetMotherboardList();
    IEnumerable<Memory> GetMemoryList();
    IEnumerable<VideoController> GetVideoControllerList();
    IEnumerable<Drive> GetDriveList();
    OS GetOperatingSystem();
}