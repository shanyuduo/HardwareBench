using HardwareBench.Core.Benchmarks;
using HardwareBench.Core.Benchmarks.Engines;

namespace HardwareBench.Tests.Benchmarks;

public class BenchmarkServiceTests
{
    [Fact]
    public async Task RunAsync_MixedEngines_ProducesExpectedResult()
    {
        // Engine A: WarmupRuns 1, 2 metrics per formal round (mem-triad scored, mem-copy unscored).
        // Warmup returns garbage to prove warmup results are discarded.
        var engineA = new FakeEngine("A", "Memory", 1,
        [
            _ => [new MetricValue("mem-triad", "MB/s", 1)],
            _ => [new MetricValue("mem-triad", "MB/s", 70000), new MetricValue("mem-copy", "MB/s", 100)],
            _ => [new MetricValue("mem-triad", "MB/s", 80000), new MetricValue("mem-copy", "MB/s", 200)],
            _ => [new MetricValue("mem-triad", "MB/s", 150000), new MetricValue("mem-copy", "MB/s", 300)],
        ]);
        // Engine B: throws on first call.
        var engineB = new FakeEngine("B", "Disk", 0, [], () => new IOException("disk failed"));
        // Engine C: 1 unscored metric.
        var engineC = new FakeEngine("C", "CPU", 0,
        [
            _ => [new MetricValue("cpu-unknown", "pts", 42)],
            _ => [new MetricValue("cpu-unknown", "pts", 42)],
            _ => [new MetricValue("cpu-unknown", "pts", 42)],
        ]);

        var guard = new FakeGuard(new FairnessReport("高性能", true, 5.0, true, []));
        var service = new BenchmarkService([engineA, engineB, engineC], guard);

        var progress = new List<BenchmarkProgress>();
        var result = await service.RunAsync(new SyncProgress(progress), CancellationToken.None);

        // Errors exactly 1, EngineId "B".
        Assert.Single(result.Errors);
        Assert.Equal("B", result.Errors[0].EngineId);

        // A's scored metric: median of 70000/80000/150000 = 80000 (mean would be 100000).
        var triad = result.Metrics.Single(m => m.Id == "mem-triad");
        Assert.Equal(3, triad.AllRuns.Length);
        Assert.Equal(80000, triad.Value);
        Assert.Equal(1000, triad.Score); // ComputeScore(80000, 80000)
        Assert.Equal("Memory", triad.Category);
        Assert.Equal("MB/s", triad.Unit);

        // A's unscored metric keeps raw runs.
        var copy = result.Metrics.Single(m => m.Id == "mem-copy");
        Assert.Equal(200, copy.Value);
        Assert.Null(copy.Score);

        // C's metric is unscored.
        var cpu = result.Metrics.Single(m => m.Id == "cpu-unknown");
        Assert.Null(cpu.Score);

        // TotalScore = GeometricMean([1000]) = 1000.
        Assert.NotNull(result.TotalScore);
        Assert.Equal(1000, result.TotalScore);

        // StartedAtUtc stamped by the service.
        Assert.True(result.StartedAtUtc > DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.True(result.StartedAtUtc <= DateTimeOffset.UtcNow);

        // Progress sequence contains warmup and final run entries for A.
        Assert.Contains(new BenchmarkProgress("A", "warmup", 1, 1), progress);
        Assert.Contains(new BenchmarkProgress("A", "run", 3, 3), progress);

        // Environment populated from fake guard + ToolVersions non-empty.
        Assert.Equal("高性能", result.Environment.PowerScheme);
        Assert.Equal(5.0, result.Environment.BackgroundCpuPercent);
        Assert.False(string.IsNullOrWhiteSpace(result.Environment.ToolVersions));
    }

    [Fact]
    public async Task RunAsync_PreCanceledToken_ThrowsOperationCanceled()
    {
        var engine = new FakeEngine("A", "Memory", 0,
        [
            _ => [new MetricValue("mem-triad", "MB/s", 1)],
            _ => [new MetricValue("mem-triad", "MB/s", 1)],
            _ => [new MetricValue("mem-triad", "MB/s", 1)],
        ]);
        var service = new BenchmarkService(
            [engine], new FakeGuard(new FairnessReport("平衡", true, 1.0, true, [])));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RunAsync(null, cts.Token));
    }

    [Fact]
    public async Task RunAsync_AllEnginesThrow_ErrorsCount2_MetricsEmpty_TotalScoreNull()
    {
        var engineA = new FakeEngine("A", "Memory", 0, [], () => new InvalidOperationException("a"));
        var engineB = new FakeEngine("B", "Disk", 0, [], () => new IOException("b"));
        var service = new BenchmarkService(
            [engineA, engineB], new FakeGuard(new FairnessReport("平衡", true, 1.0, true, [])));

        var result = await service.RunAsync(null, CancellationToken.None);

        Assert.Equal(2, result.Errors.Count);
        Assert.Empty(result.Metrics);
        Assert.Null(result.TotalScore);
    }

    [Fact]
    public async Task RunAsync_GuardThrowsNonOce_RecordsFairnessGuardError_AndCompletes()
    {
        var engine = new FakeEngine("A", "Memory", 0,
        [
            _ => [new MetricValue("mem-triad", "MB/s", 80000)],
            _ => [new MetricValue("mem-triad", "MB/s", 80000)],
            _ => [new MetricValue("mem-triad", "MB/s", 80000)],
        ]);
        var guard = new FakeGuard(
            new FairnessReport("平衡", true, 1.0, true, []),
            () => new InvalidOperationException("guard down"));
        var service = new BenchmarkService([engine], guard);

        var result = await service.RunAsync(null, CancellationToken.None);

        Assert.Contains(result.Errors, e => e.EngineId == "fairness.guard");
        Assert.Single(result.Metrics);
        Assert.Equal("未知", result.Environment.PowerScheme);
        Assert.Equal(-1, result.Environment.BackgroundCpuPercent);
    }

    [Fact]
    public async Task RunAsync_EngineInconsistentMetricSets_RecordsError_AndSkipsEngine()
    {
        var engine = new FakeEngine("A", "Memory", 0,
        [
            _ => [new MetricValue("mem-triad", "MB/s", 1)],
            _ => [new MetricValue("mem-copy", "MB/s", 2)], // different id in round 2
            _ => [new MetricValue("mem-triad", "MB/s", 3)],
        ]);
        var service = new BenchmarkService(
            [engine], new FakeGuard(new FairnessReport("平衡", true, 1.0, true, [])));

        var result = await service.RunAsync(null, CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Equal("A", result.Errors[0].EngineId);
        Assert.Empty(result.Metrics);
        Assert.Null(result.TotalScore);
    }

    [Fact]
    public async Task RunAsync_WarmupResultsAreDiscarded_MetricsReflectFormalRunsOnly()
    {
        var engine = new FakeEngine("A", "Memory", 1,
        [
            _ => [new MetricValue("mem-triad", "MB/s", 1), new MetricValue("mem-copy", "MB/s", 1)],
            _ => [new MetricValue("mem-triad", "MB/s", 70000), new MetricValue("mem-copy", "MB/s", 100)],
            _ => [new MetricValue("mem-triad", "MB/s", 80000), new MetricValue("mem-copy", "MB/s", 200)],
            _ => [new MetricValue("mem-triad", "MB/s", 150000), new MetricValue("mem-copy", "MB/s", 300)],
        ]);
        var service = new BenchmarkService(
            [engine], new FakeGuard(new FairnessReport("平衡", true, 2.0, true, [])));

        var result = await service.RunAsync(null, CancellationToken.None);

        var triad = result.Metrics.Single(m => m.Id == "mem-triad");
        Assert.Equal(80000, triad.Value);
        Assert.Equal([70000, 80000, 150000], triad.AllRuns);
    }

    [Fact]
    public async Task RunAsync_EngineThrowsOce_Rethrows_NoErrorEntry()
    {
        var engine = new FakeEngine("A", "Memory", 0, [], () => new OperationCanceledException("canceled"));
        var service = new BenchmarkService(
            [engine], new FakeGuard(new FairnessReport("平衡", true, 1.0, true, [])));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RunAsync(null, CancellationToken.None));
    }

    private sealed class SyncProgress : IProgress<BenchmarkProgress>
    {
        private readonly List<BenchmarkProgress> _items;

        public SyncProgress(List<BenchmarkProgress> items) => _items = items;

        public void Report(BenchmarkProgress value) => _items.Add(value);
    }

    private sealed class FakeEngine : IBenchmarkEngine
    {
        private readonly Queue<Func<int, IReadOnlyList<MetricValue>>> _responses;
        private readonly Func<Exception>? _throwOnCall;
        private int _callCount;

        public FakeEngine(
            string id,
            string category,
            int warmupRuns,
            IEnumerable<Func<int, IReadOnlyList<MetricValue>>> responses,
            Func<Exception>? throwOnCall = null)
        {
            Id = id;
            Category = category;
            WarmupRuns = warmupRuns;
            _responses = new Queue<Func<int, IReadOnlyList<MetricValue>>>(responses);
            _throwOnCall = throwOnCall;
        }

        public string Id { get; }
        public string Category { get; }
        public int WarmupRuns { get; }

        public Task<IReadOnlyList<MetricValue>> RunOnceAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_throwOnCall is not null)
                throw _throwOnCall();
            return Task.FromResult(_responses.Dequeue()(_callCount++));
        }
    }

    private sealed class FakeGuard : IFairnessGuard
    {
        private readonly FairnessReport _report;
        private readonly Func<Exception>? _throwOnCall;

        public FakeGuard(FairnessReport report, Func<Exception>? throwOnCall = null)
        {
            _report = report;
            _throwOnCall = throwOnCall;
        }

        public Task<FairnessReport> CheckAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_throwOnCall is not null)
                throw _throwOnCall();
            return Task.FromResult(_report);
        }
    }
}