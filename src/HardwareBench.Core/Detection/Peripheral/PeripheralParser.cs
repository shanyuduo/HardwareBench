using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection.Peripheral;

public sealed record RawEnumEntry(
    string Branch, string InstancePath, string? DeviceDesc, string? ClassGuid);

public static class PeripheralParser
{
    private static readonly (string Guid, string Kind)[] KnownClasses =
    [
        ("{4d36e96f-e325-11ce-bfc1-08002be10318}", "Mouse"),
        ("{4d36e96b-e325-11ce-bfc1-08002be10318}", "Keyboard"),
        ("{4d36e96c-e325-11ce-bfc1-08002be10318}", "Media"),
        ("{745a17a0-74d3-11d0-b6fe-00a0c90f57da}", "HID 设备"),
        ("{6d807884-7d21-11cf-801e-08002be10318}", "Printer"),
    ];

    public static List<PeripheralInfo> Parse(IEnumerable<RawEnumEntry> entries)
    {
        var result = new List<PeripheralInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            var kind = KnownClasses
                .FirstOrDefault(c => string.Equals(c.Guid, e.ClassGuid, StringComparison.OrdinalIgnoreCase)).Kind;
            if (kind is null) continue;
            if (!seen.Add(e.InstancePath)) continue;
            string name = CleanName(e.DeviceDesc) ?? e.InstancePath;
            result.Add(new PeripheralInfo(kind, name, e.InstancePath));
        }
        return result;
    }

    internal static string? CleanName(string? raw)
    {
        if (raw is null) return null;
        int semi = raw.IndexOf(';');
        return raw.Length > 0 && raw[0] == '@' && semi >= 0 ? raw[(semi + 1)..] : raw;
    }
}