using System.Buffers.Binary;

namespace HardwareBench.Core.Detection.Storage;

public sealed record ParsedDescriptor(
    string? Vendor, string? Product, string? Revision, string? Serial,
    uint BusType, bool IncursSeekPenalty, int? TemperatureC);

public static class StorageDescriptorParser
{
    public static ParsedDescriptor Parse(byte[] buffer, bool incursSeekPenalty = false, int? temperatureC = null)
    {
        if (buffer.Length < 36)
            throw new ArgumentException("缓冲区小于 STORAGE_DEVICE_DESCRIPTOR 固定头。", nameof(buffer));

        string? ReadString(uint offset) => offset == 0 || offset >= buffer.Length
            ? null
            : ReadNullTerminated(buffer, (int)offset);

        return new ParsedDescriptor(
            Vendor: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(12))),
            Product: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(16))),
            Revision: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(20))),
            Serial: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(24))),
            BusType: BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(28)),
            IncursSeekPenalty: incursSeekPenalty,
            TemperatureC: temperatureC);
    }

    private static string? ReadNullTerminated(byte[] buffer, int offset)
    {
        int end = offset;
        while (end < buffer.Length && buffer[end] != 0) end++;
        var s = System.Text.Encoding.ASCII.GetString(buffer, offset, end - offset).TrimEnd();
        return s.Length == 0 ? null : s;
    }
}