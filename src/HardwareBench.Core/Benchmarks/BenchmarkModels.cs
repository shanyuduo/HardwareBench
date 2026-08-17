namespace HardwareBench.Core.Benchmarks;

public sealed record MetricValue(string Id, string Unit, double Value);

public sealed record MetricResult(string Id, string Category, string Unit, double Value, int? Score, double[] AllRuns);

public sealed record BenchmarkError(string EngineId, string Message);

public sealed record BenchmarkEnvironment(string PowerScheme, double BackgroundCpuPercent, string ToolVersions);

public sealed class BenchmarkResult
{
    public DateTimeOffset StartedAtUtc { get; set; }
    public BenchmarkEnvironment Environment { get; set; } = null!;
    public List<MetricResult> Metrics { get; set; } = [];
    public List<BenchmarkError> Errors { get; set; } = [];
    public int? TotalScore { get; set; }
}