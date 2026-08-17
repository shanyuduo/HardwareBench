namespace HardwareBench.Core.Detection.Edid;

public sealed record EdidData(
    string ManufacturerId, ushort ProductCode, uint SerialNumber,
    byte MadeWeek, ushort MadeYear, byte VersionMajor, byte VersionMinor,
    string? ModelName, string? SerialString, byte ExtensionCount, bool ChecksumValid);