namespace HardwareBench.Core.Models;

public sealed record CpuInfo(
    string Name, string? ProcessorId, int PhysicalCores, int LogicalProcessors,
    int MaxClockSpeedMhz, long? L3CacheKb);

public sealed record MotherboardInfo(string Manufacturer, string Product, string? SerialNumber);

public sealed record MemoryModule(
    string BankLabel, string DeviceLocator, ulong CapacityBytes, int SpeedMts,
    string? Manufacturer, string? PartNumber, string? SerialNumber, string? FormFactor);

public sealed record GpuInfo(
    string Name, string? Vendor, ulong? DedicatedMemoryBytes,
    string? DriverVersion, string? VideoProcessor);