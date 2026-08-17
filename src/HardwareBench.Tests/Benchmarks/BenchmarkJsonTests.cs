using HardwareBench.Core.Benchmarks;

namespace HardwareBench.Tests.Benchmarks;

public class BenchmarkJsonTests
{
    [Fact]
    public void RoundTrip_FullResult_PreservesAllFields()
    {
        var result = new BenchmarkResult
        {
            StartedAtUtc = new DateTimeOffset(2026, 8, 17, 14, 30, 0, TimeSpan.Zero),
            Environment = new BenchmarkEnvironment("Balanced", 2.5, "CPU-Z 2.10; GPU-Z 2.60", ["警告一", "警告二"]),
            Metrics =
            {
                new MetricResult("cpu.multi", "CPU", "points", 18425.3, 512, [18200.0, 18425.3, 18350.7]),
                new MetricResult("gpu.fps", "GPU", "FPS", 142.0, null, [140.5, 142.0, 141.2])
            },
            Errors = { new BenchmarkError("cpu.engine", "thermal throttling observed") },
            TotalScore = 512
        };

        var json = BenchmarkJson.Serialize(result);
        var back = BenchmarkJson.Deserialize(json);

        Assert.Equal(result.StartedAtUtc, back.StartedAtUtc);
        Assert.Equal(result.Environment.PowerScheme, back.Environment.PowerScheme);
        Assert.Equal(result.Environment.BackgroundCpuPercent, back.Environment.BackgroundCpuPercent);
        Assert.Equal(result.Environment.ToolVersions, back.Environment.ToolVersions);
        Assert.Equal(2, back.Environment.Warnings!.Count);
        Assert.Equal("警告一", back.Environment.Warnings[0]);
        Assert.Equal("警告二", back.Environment.Warnings[1]);

        Assert.Equal(result.Metrics[0].Id, back.Metrics[0].Id);
        Assert.Equal(result.Metrics[0].Category, back.Metrics[0].Category);
        Assert.Equal(result.Metrics[0].Unit, back.Metrics[0].Unit);
        Assert.Equal(result.Metrics[0].Value, back.Metrics[0].Value);
        Assert.Equal(result.Metrics[0].Score, back.Metrics[0].Score);
        Assert.Equal(result.Metrics[0].AllRuns, back.Metrics[0].AllRuns);

        Assert.Equal(result.Metrics[1].Id, back.Metrics[1].Id);
        Assert.Equal(result.Metrics[1].Category, back.Metrics[1].Category);
        Assert.Equal(result.Metrics[1].Unit, back.Metrics[1].Unit);
        Assert.Equal(result.Metrics[1].Value, back.Metrics[1].Value);
        Assert.Null(back.Metrics[1].Score);
        Assert.Equal(result.Metrics[1].AllRuns, back.Metrics[1].AllRuns);

        Assert.Equal(result.Errors[0].EngineId, back.Errors[0].EngineId);
        Assert.Equal(result.Errors[0].Message, back.Errors[0].Message);

        Assert.Equal(result.TotalScore, back.TotalScore);
    }

    [Fact]
    public void RoundTrip_TotalScoreNull_StaysNull()
    {
        var result = new BenchmarkResult
        {
            StartedAtUtc = new DateTimeOffset(2026, 8, 17, 15, 0, 0, TimeSpan.Zero),
            Environment = new BenchmarkEnvironment("High Performance", 0.5, "CPU-Z 2.10"),
            Metrics =
            {
                new MetricResult("cpu.single", "CPU", "points", 2100.0, null, [2080.0, 2100.0])
            },
            TotalScore = null
        };

        var json = BenchmarkJson.Serialize(result);
        var back = BenchmarkJson.Deserialize(json);

        Assert.Null(back.TotalScore);
        Assert.Null(back.Environment.Warnings);
    }
}