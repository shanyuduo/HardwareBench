using System.Runtime.Versioning;
using HardwareBench.Core.Benchmarks.Parsers;

namespace HardwareBench.Core.Benchmarks.Engines;

[SupportedOSPlatform("windows")]
public sealed class SevenZipEngine : IBenchmarkEngine
{
    private const string ToolName = "7zr.exe";
    private const int RunTimeoutMs = 600_000;

    private readonly IToolLocator _tools;
    private readonly IProcessRunner _runner;

    public SevenZipEngine(IToolLocator tools, IProcessRunner runner)
    {
        _tools = tools;
        _runner = runner;
    }

    public string Id => "engine.cpu7z";
    public string Category => "Cpu";
    public int WarmupRuns => 0;

    public async Task<IReadOnlyList<MetricValue>> RunOnceAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var exe = _tools.GetToolPath(ToolName);
        var spec = new ProcessSpec(exe, ["b"], RunTimeoutMs, Path.GetDirectoryName(exe)!);
        var result = await _runner.RunAsync(spec, ct);

        if (result.TimedOut || result.ExitCode != 0)
        {
            var stderr = result.StdErr.Length > 200 ? result.StdErr[..200] : result.StdErr;
            throw new InvalidOperationException($"7-Zip exited {result.ExitCode}: {stderr}");
        }

        var metrics = SevenZipParser.Parse(result.StdOut);
        return
        [
            new MetricValue("cpu-7z-rating", "MIPS", metrics.TotalRatingMips),
            new MetricValue("cpu-7z-compress", "MIPS", metrics.CompressRatingMips),
            new MetricValue("cpu-7z-decompress", "MIPS", metrics.DecompressRatingMips),
        ];
    }
}