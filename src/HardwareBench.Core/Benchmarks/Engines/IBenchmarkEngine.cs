namespace HardwareBench.Core.Benchmarks.Engines;

public interface IBenchmarkEngine
{
    string Id { get; }
    string Category { get; }
    int WarmupRuns { get; }
    Task<IReadOnlyList<MetricValue>> RunOnceAsync(CancellationToken ct);
}