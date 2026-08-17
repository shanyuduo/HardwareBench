using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace HardwareBench.Core.Benchmarks;

public sealed record FairnessReport(
    string PowerSchemeName,
    bool PowerSchemeOk,
    double BackgroundCpuPercent,
    bool BackgroundCpuOk,
    IReadOnlyList<string> Warnings);

public interface IFairnessGuard
{
    Task<FairnessReport> CheckAsync(CancellationToken ct);
}

public static class PowerSchemeParser
{
    private static readonly Regex SchemeRegex = new(
        @"GUID:\s*([0-9a-fA-F-]{36})\s*\((.+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static (string Guid, string Name)? Parse(string powercfgOutput)
    {
        if (string.IsNullOrWhiteSpace(powercfgOutput))
            return null;

        var match = SchemeRegex.Match(powercfgOutput);
        if (!match.Success)
            return null;

        return (match.Groups[1].Value, match.Groups[2].Value.Trim());
    }
}

[SupportedOSPlatform("windows")]
public static class CpuBusySampler
{
    public static double SampleBusyFraction(int intervalMs)
    {
        if (!GetSystemTimes(out var idle1, out var kernel1, out var user1))
            return 0;

        Thread.Sleep(intervalMs);

        if (!GetSystemTimes(out var idle2, out var kernel2, out var user2))
            return 0;

        var idleDelta = ToUInt64(idle2) - ToUInt64(idle1);
        var kernelDelta = ToUInt64(kernel2) - ToUInt64(kernel1);
        var userDelta = ToUInt64(user2) - ToUInt64(user1);

        var total = idleDelta + kernelDelta + userDelta;
        if (total == 0)
            return 0;

        var busy = 1.0 - (double)idleDelta / total;
        return Math.Clamp(busy, 0.0, 1.0);
    }

    private static ulong ToUInt64(FILETIME ft) =>
        ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);
}

[SupportedOSPlatform("windows")]
public sealed class FairnessGuard : IFairnessGuard
{
    private static readonly string[] OkSchemeKeywords = { "平衡", "Balanced", "高性能", "High performance" };

    private readonly IProcessRunner _runner;

    public FairnessGuard(IProcessRunner runner) => _runner = runner;

    public async Task<FairnessReport> CheckAsync(CancellationToken ct)
    {
        var warnings = new List<string>();
        var schemeName = "未知";
        var schemeOk = false;

        try
        {
            var spec = new ProcessSpec(
                "powercfg.exe", new[] { "/getactivescheme" }, 5000, Environment.CurrentDirectory);
            var result = await _runner.RunAsync(spec, ct);

            var parsed = PowerSchemeParser.Parse(result.StdOut);
            if (parsed is { } p)
            {
                schemeName = p.Name;
                schemeOk = IsOkScheme(p.Name);
                if (!schemeOk)
                    warnings.Add($"电源计划为 {p.Name}，建议切换到平衡/高性能");
            }
            else
            {
                warnings.Add("电源计划为 未知，建议切换到平衡/高性能");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            warnings.Add("无法读取当前电源计划，建议手动确认已切换到平衡/高性能");
        }

        var cpuPercent = -1.0;
        var cpuOk = false;
        try
        {
            var fraction = CpuBusySampler.SampleBusyFraction(300);
            cpuPercent = fraction * 100;
            cpuOk = cpuPercent <= 15;
            if (!cpuOk)
                warnings.Add($"后台 CPU 占用 {cpuPercent:F0}%，建议关闭占用程序后重测");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            cpuPercent = -1;
            warnings.Add("无法采样后台 CPU 占用，建议手动确认系统空闲");
        }

        return new FairnessReport(schemeName, schemeOk, cpuPercent, cpuOk, warnings);
    }

    private static bool IsOkScheme(string name)
    {
        var normalized = name.Replace(" ", string.Empty);
        foreach (var keyword in OkSchemeKeywords)
        {
            if (normalized.Contains(keyword.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}