using System.Text.Json;
using System.Text.Json.Serialization;

namespace HardwareBench.Core.Benchmarks;

public static class BenchmarkJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(BenchmarkResult result)
        => JsonSerializer.Serialize(result, Options);

    public static BenchmarkResult Deserialize(string json)
        => JsonSerializer.Deserialize<BenchmarkResult>(json)
           ?? throw new JsonException("反序列化结果为 null。");
}