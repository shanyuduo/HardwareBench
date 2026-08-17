using System.Text.Json;
using System.Text.Json.Serialization;
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Serialization;

public static class ReportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(HardwareReport report)
        => JsonSerializer.Serialize(report, Options);

    public static HardwareReport Deserialize(string json)
        => JsonSerializer.Deserialize<HardwareReport>(json)
           ?? throw new JsonException("反序列化结果为 null。");
}