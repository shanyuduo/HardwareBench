using System.Diagnostics;
using HardwareBench.Core.Benchmarks;
using HardwareBench.Core.Benchmarks.Engines;
using Xunit.Abstractions;

namespace HardwareBench.Tests.Benchmarks;

[Trait("Category", "Integration")]
public class EnginesIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public EnginesIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static (ToolExtractor Tools, BoundedProcessRunner Runner) CreateStack()
        => (new ToolExtractor(new AssemblyResourceBinariesSource()), new BoundedProcessRunner());

    [Fact]
    public async Task DiskSpdEngine_RunOnce_ReturnsFourMetricsWithExpectedIdsUnitsAndPositiveValues()
    {
        var (tools, runner) = CreateStack();
        var engine = new DiskSpdEngine(tools, runner);

        var sw = Stopwatch.StartNew();
        var metrics = await engine.RunOnceAsync(CancellationToken.None);
        sw.Stop();
        _output.WriteLine($"[runtime] DiskSpdEngine.RunOnceAsync took {sw.Elapsed.TotalSeconds:F1}s");

        Assert.Equal(4, metrics.Count);
        Assert.Equal("disk-seq-read", metrics[0].Id);
        Assert.Equal("MiB/s", metrics[0].Unit);
        Assert.Equal("disk-seq-iops", metrics[1].Id);
        Assert.Equal("IOPS", metrics[1].Unit);
        Assert.Equal("disk-4k-read", metrics[2].Id);
        Assert.Equal("MiB/s", metrics[2].Unit);
        Assert.Equal("disk-4k-iops", metrics[3].Id);
        Assert.Equal("IOPS", metrics[3].Unit);

        Assert.True(metrics[0].Value > 10,
            $"disk-seq-read = {metrics[0].Value:F1} MiB/s, expected > 10");
        Assert.True(metrics[1].Value > 0, "disk-seq-iops must be > 0");
        Assert.True(metrics[2].Value > 0, "disk-4k-read must be > 0");
        Assert.True(metrics[3].Value > 0, "disk-4k-iops must be > 0");
    }

    [Fact]
    public async Task SevenZipEngine_RunOnce_ReturnsThreeMetricsWithExpectedIdsUnitsAndPositiveValues()
    {
        var (tools, runner) = CreateStack();
        var engine = new SevenZipEngine(tools, runner);

        var sw = Stopwatch.StartNew();
        var metrics = await engine.RunOnceAsync(CancellationToken.None);
        sw.Stop();
        _output.WriteLine($"[runtime] SevenZipEngine.RunOnceAsync took {sw.Elapsed.TotalSeconds:F1}s");

        Assert.Equal(3, metrics.Count);
        Assert.Equal("cpu-7z-rating", metrics[0].Id);
        Assert.Equal("MIPS", metrics[0].Unit);
        Assert.Equal("cpu-7z-compress", metrics[1].Id);
        Assert.Equal("MIPS", metrics[1].Unit);
        Assert.Equal("cpu-7z-decompress", metrics[2].Id);
        Assert.Equal("MIPS", metrics[2].Unit);

        Assert.True(metrics[0].Value > 0, "cpu-7z-rating must be > 0");
        Assert.True(metrics[1].Value > 0, "cpu-7z-compress must be > 0");
        Assert.True(metrics[2].Value > 0, "cpu-7z-decompress must be > 0");
    }

    [Fact]
    public async Task MemoryStreamEngine_RunOnce_ReturnsFourMetricsWithExpectedIdsUnitsAndPositiveValues()
    {
        var (tools, runner) = CreateStack();
        var engine = new MemoryStreamEngine(tools, runner);

        var sw = Stopwatch.StartNew();
        var metrics = await engine.RunOnceAsync(CancellationToken.None);
        sw.Stop();
        _output.WriteLine($"[runtime] MemoryStreamEngine.RunOnceAsync took {sw.Elapsed.TotalSeconds:F1}s");

        Assert.Equal(4, metrics.Count);
        Assert.Equal("mem-triad", metrics[0].Id);
        Assert.Equal("MB/s", metrics[0].Unit);
        Assert.Equal("mem-copy", metrics[1].Id);
        Assert.Equal("MB/s", metrics[1].Unit);
        Assert.Equal("mem-scale", metrics[2].Id);
        Assert.Equal("MB/s", metrics[2].Unit);
        Assert.Equal("mem-add", metrics[3].Id);
        Assert.Equal("MB/s", metrics[3].Unit);

        Assert.True(metrics[0].Value > 0, "mem-triad must be > 0");
        Assert.True(metrics[1].Value > 0, "mem-copy must be > 0");
        Assert.True(metrics[2].Value > 0, "mem-scale must be > 0");
        Assert.True(metrics[3].Value > 0, "mem-add must be > 0");
    }

    [Fact]
    public async Task AllEngines_PreCanceledToken_ThrowsOperationCanceledException()
    {
        var (tools, runner) = CreateStack();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        IBenchmarkEngine[] engines =
        [
            new DiskSpdEngine(tools, runner),
            new SevenZipEngine(tools, runner),
            new MemoryStreamEngine(tools, runner),
        ];

        foreach (var engine in engines)
            await Assert.ThrowsAsync<OperationCanceledException>(() => engine.RunOnceAsync(cts.Token));
    }
}