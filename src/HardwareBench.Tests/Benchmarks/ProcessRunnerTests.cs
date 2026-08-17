using System.Diagnostics;
using HardwareBench.Core.Benchmarks;

namespace HardwareBench.Tests.Benchmarks;

public class ProcessRunnerTests
{
    [Trait("Category", "Integration")]
    [Fact]
    public async Task RunAsync_NormalExit_ReturnsExitCodeAndStdOut()
    {
        var runner = new BoundedProcessRunner();
        var spec = new ProcessSpec("cmd.exe", new[] { "/c", "echo", "hello-bench" }, 5000, "");
        var result = await runner.RunAsync(spec, CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello-bench", result.StdOut);
        Assert.False(result.TimedOut);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task RunAsync_Timeout_KillsProcessAndSetsTimedOut()
    {
        var runner = new BoundedProcessRunner();
        var spec = new ProcessSpec("cmd.exe", new[] { "/c", "ping", "-n", "30", "127.0.0.1" }, 1500, "");
        var sw = Stopwatch.StartNew();
        var result = await runner.RunAsync(spec, CancellationToken.None);
        sw.Stop();

        Assert.True(result.TimedOut);
        Assert.True(sw.Elapsed.TotalSeconds < 10,
            $"RunAsync took {sw.Elapsed.TotalSeconds:F1}s, expected <10s");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task RunAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        var runner = new BoundedProcessRunner();
        var spec = new ProcessSpec("cmd.exe", new[] { "/c", "echo", "hello" }, 5000, "");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => runner.RunAsync(spec, cts.Token));
    }
}