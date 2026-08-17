namespace HardwareBench.Core.Models;

public sealed record StorageDevice(
    string DevicePath, string Model, string? SerialNumber, string? FirmwareRevision,
    string BusType, bool? IsSsd, int? TemperatureC, ulong? SizeBytes);

public sealed record MonitorInfo(
    string InstancePath, string ManufacturerId, ushort ProductCode,
    string? ModelName, string? SerialString, ushort? MadeYear, byte? MadeWeek);

public sealed record PeripheralInfo(string Kind, string Name, string InstancePath);