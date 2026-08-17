using System.Runtime.Versioning;
using HardwareBench.Core.Detection.Wmi;
using HardwareBench.Core.Models;
using Hardware.Info;

namespace HardwareBench.Tests.Detection;

[SupportedOSPlatform("windows")]
public class WmiInventoryDetectorTests
{
    private sealed class FakeSource : IWmiSource
    {
        public List<CPU> Cpus { get; } = [];
        public List<Motherboard> Boards { get; } = [];
        public List<Memory> Memory { get; } = [];
        public List<VideoController> Gpus { get; } = [];
        public List<Drive> Drives { get; } = [];
        public OS? Os { get; set; }
        public bool ThrowCpu { get; set; }
        public bool ThrowMemory { get; set; }

        public IEnumerable<CPU> GetCpuList() { if (ThrowCpu) throw new InvalidOperationException("wmi down"); return Cpus; }
        public IEnumerable<Motherboard> GetMotherboardList() => Boards;
        public IEnumerable<Memory> GetMemoryList() { if (ThrowMemory) throw new InvalidOperationException("wmi down"); return Memory; }
        public IEnumerable<VideoController> GetVideoControllerList() => Gpus;
        public IEnumerable<Drive> GetDriveList() => Drives;
        public OS GetOperatingSystem() => Os!;
    }

    [Fact]
    public async Task Detect_MapsAllSections()
    {
        var src = new FakeSource
        {
            Cpus = { new CPU { Name = "Intel Core i7-13700K", NumberOfCores = 16, NumberOfLogicalProcessors = 24, MaxClockSpeed = 5400 } },
            Boards = { new Motherboard { Manufacturer = "ASUSTeK", Product = "ROG STRIX Z790-E" } },
            Memory = { new Memory { BankLabel = "BANK 0", Capacity = 34359738368, Speed = 5600 } },
            Gpus = { new VideoController { Name = "NVIDIA GeForce RTX 4070", DriverVersion = "32.0.15.6094" } },
            Os = new OS { Name = "Microsoft Windows 11 Pro", VersionString = "10.0.26100", Version = new Version(10, 0, 26100) }
        };
        var report = new HardwareReport();

        await new WmiInventoryDetector(src).DetectAsync(report, CancellationToken.None);

        Assert.NotNull(report.Os);
        Assert.Equal("Intel Core i7-13700K", report.Cpu!.Name);
        Assert.Equal(24, report.Cpu.LogicalProcessors);
        Assert.Equal("ROG STRIX Z790-E", report.Motherboard!.Product);
        Assert.Single(report.MemoryModules);
        Assert.Equal(34359738368ul, report.MemoryModules[0].CapacityBytes);
        Assert.Equal("NVIDIA GeForce RTX 4070", report.Gpus[0].Name);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task Detect_SectionFailure_RecordsErrorAndContinues()
    {
        var src = new FakeSource { ThrowCpu = true, ThrowMemory = true };
        var report = new HardwareReport();

        await new WmiInventoryDetector(src).DetectAsync(report, CancellationToken.None);

        Assert.Null(report.Cpu);
        Assert.Empty(report.MemoryModules);
        Assert.Equal(2, report.Errors.Count);
        Assert.Contains(report.Errors, e => e.DetectorId == "wmi.inventory");
    }
}