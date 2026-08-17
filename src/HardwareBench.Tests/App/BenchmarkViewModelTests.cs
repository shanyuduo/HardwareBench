using HardwareBench.App.ViewModels;
using HardwareBench.Core.Benchmarks;

namespace HardwareBench.Tests.App;

public class BenchmarkViewModelTests
{
    private sealed class FakeService : IBenchmarkService
    {
        public Exception? ToThrow { get; set; }
        public IReadOnlyList<string>? Warnings { get; set; } = ["测试警告"];

        public Task<BenchmarkResult> RunAsync(IProgress<BenchmarkProgress>? progress, CancellationToken ct)
        {
            if (ToThrow is not null)
                throw ToThrow;

            return Task.FromResult(new BenchmarkResult
            {
                StartedAtUtc = DateTimeOffset.UtcNow,
                Environment = new BenchmarkEnvironment("平衡", 0, "test", Warnings),
                Metrics =
                {
                    new MetricResult("mem-triad", "Memory", "MB/s", 80000, 1000, [80000, 80000, 80000]),
                    new MetricResult("mem-copy", "Memory", "MB/s", 33000, null, [33000, 33000, 33000]),
                },
                TotalScore = 512,
            });
        }
    }

    private sealed class FakeExporter : IFileSaveService
    {
        public string? PathToReturn { get; set; }

        public string? PickSavePath(string defaultName)
            => PathToReturn ?? Path.Combine(Path.GetTempPath(), defaultName);
    }

    [Fact]
    public async Task Start_PopulatesResults()
    {
        var vm = new BenchmarkViewModel(new FakeService(), new FakeExporter());

        await vm.StartCommand.ExecuteAsync(null);

        Assert.False(vm.IsBusy);
        Assert.True(vm.HasResults);
        Assert.Equal(2, vm.MetricRows.Count);
        Assert.Equal("mem-triad", vm.MetricRows[0].Id);
        Assert.Equal("80000.0 MB/s", vm.MetricRows[0].RawDisplay);
        Assert.Equal("1000 / 1000", vm.MetricRows[0].ScoreDisplay);
        Assert.Equal("mem-copy", vm.MetricRows[1].Id);
        Assert.Equal("33000.0 MB/s", vm.MetricRows[1].RawDisplay);
        Assert.Equal("—", vm.MetricRows[1].ScoreDisplay);
        Assert.Contains("512", vm.TotalText);
        Assert.Contains("2026.08-m2", vm.TotalText);
        Assert.Contains("测试警告", vm.FairnessText);
        Assert.Equal("跑分完成", vm.ProgressText);
    }

    [Fact]
    public async Task Export_WritesJsonRoundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hb-bench-{Guid.NewGuid():N}.json");
        var exporter = new FakeExporter { PathToReturn = path };
        var vm = new BenchmarkViewModel(new FakeService(), exporter);
        await vm.StartCommand.ExecuteAsync(null);

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.True(File.Exists(path));
        var result = BenchmarkJson.Deserialize(File.ReadAllText(path));
        Assert.Equal(512, result.TotalScore);
        Assert.Equal(2, result.Metrics.Count);
        File.Delete(path);
    }

    [Fact]
    public async Task Start_NoWarnings_ShowsEnvironmentCheckPassed()
    {
        var vm = new BenchmarkViewModel(new FakeService { Warnings = null }, new FakeExporter());

        await vm.StartCommand.ExecuteAsync(null);

        Assert.Equal("环境检查通过", vm.FairnessText);
    }

    [Fact]
    public async Task Start_ServiceThrows_ShowsErrorWithoutCrash()
    {
        var vm = new BenchmarkViewModel(
            new FakeService { ToThrow = new InvalidOperationException("boom") }, new FakeExporter());

        await vm.StartCommand.ExecuteAsync(null);

        Assert.False(vm.IsBusy);
        Assert.Contains("boom", vm.ProgressText);
    }
}