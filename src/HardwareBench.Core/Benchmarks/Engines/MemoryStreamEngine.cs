using System.Runtime.Versioning;
using HardwareBench.Core.Benchmarks.Parsers;

namespace HardwareBench.Core.Benchmarks.Engines;

[SupportedOSPlatform("windows")]
public sealed class MemoryStreamEngine : IBenchmarkEngine
{
    private const string ToolName = "stream-windows.exe";
    private const int RunTimeoutMs = 180_000;

    private readonly IToolLocator _tools;
    private readonly IProcessRunner _runner;

    public MemoryStreamEngine(IToolLocator tools, IProcessRunner runner)
    {
        _tools = tools;
        _runner = runner;
    }

    public string Id => "engine.memstream";
    public string Category => "Memory";
    public int WarmupRuns => 0;

    public async Task<IReadOnlyList<MetricValue>> RunOnceAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var exe = _tools.GetToolPath(ToolName);
        var spec = new ProcessSpec(exe, [], RunTimeoutMs, Path.GetDirectoryName(exe)!);
        var result = await _runner.RunAsync(spec, ct);

        if (result.TimedOut || result.ExitCode != 0)
        {
            var stderr = result.StdErr.Length > 200 ? result.StdErr[..200] : result.StdErr;
            throw new InvalidOperationException($"STREAM exited {result.ExitCode}: {stderr}");
        }

        var metrics = StreamParser.Parse(result.StdOut);
        return
        [
            new MetricValue("mem-triad", "MB/s", metrics.Triad),
            new MetricValue("mem-copy", "MB/s", metrics.Copy),
            new MetricValue("mem-scale", "MB/s", metrics.Scale),
            new MetricValue("mem-add", "MB/s", metrics.Add),
        ];
    }
}