using System.Runtime.Versioning;
using HardwareBench.Core.Benchmarks.Parsers;

namespace HardwareBench.Core.Benchmarks.Engines;

[SupportedOSPlatform("windows")]
public sealed class DiskSpdEngine : IBenchmarkEngine
{
    private const string ToolName = "diskspd.exe";
    private const int RunTimeoutMs = 60_000;
    private const int NormalDurationSeconds = 5;

    private readonly IToolLocator _tools;
    private readonly IProcessRunner _runner;

    public DiskSpdEngine(IToolLocator tools, IProcessRunner runner)
    {
        _tools = tools;
        _runner = runner;
    }

    public string Id => "engine.disk";
    public string Category => "Disk";
    public int WarmupRuns => 1;

    public async Task<IReadOnlyList<MetricValue>> RunOnceAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var workDir = Path.Combine(Path.GetTempPath(), "HardwareBench", "work");
        Directory.CreateDirectory(workDir);

        var seqFile = Path.Combine(workDir, "diskspd-seq.dat");
        var rndFile = Path.Combine(workDir, "diskspd-4k.dat");

        try
        {
            var seq = await RunDiskSpdAsync(seqFile, ["-b1M"], NormalDurationSeconds, ct);
            var rnd = await RunDiskSpdAsync(rndFile, ["-b4K", "-r4K"], NormalDurationSeconds, ct);

            return
            [
                new MetricValue("disk-seq-read", "MiB/s", seq.ReadMiBS),
                new MetricValue("disk-seq-iops", "IOPS", seq.ReadIops),
                new MetricValue("disk-4k-read", "MiB/s", rnd.ReadMiBS),
                new MetricValue("disk-4k-iops", "IOPS", rnd.ReadIops),
            ];
        }
        finally
        {
            TryDelete(seqFile);
            TryDelete(rndFile);
        }
    }

    private async Task<DiskSpdMetrics> RunDiskSpdAsync(
        string file, IReadOnlyList<string> blockArgs, int durationSeconds, CancellationToken ct)
    {
        var exe = _tools.GetToolPath(ToolName);
        var args = new List<string> { "-c1G", $"-d{durationSeconds}", "-w0" };
        args.AddRange(blockArgs);
        args.AddRange(["-o8", "-t1", "-Sh", file]);

        var spec = new ProcessSpec(exe, args, RunTimeoutMs, Path.GetDirectoryName(exe)!);
        var result = await _runner.RunAsync(spec, ct);

        if (result.TimedOut || result.ExitCode != 0)
        {
            var stderr = result.StdErr.Length > 200 ? result.StdErr[..200] : result.StdErr;
            throw new InvalidOperationException($"DiskSpd exited {result.ExitCode}: {stderr}");
        }

        return DiskSpdParser.Parse(result.StdOut);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}