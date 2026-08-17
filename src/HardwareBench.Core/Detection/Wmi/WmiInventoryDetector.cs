using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using HardwareBench.Core.Models;
using Hardware.Info;

namespace HardwareBench.Core.Detection.Wmi;

[SupportedOSPlatform("windows")]
public sealed class WmiInventoryDetector(IWmiSource source)
{
    public string Id => "wmi.inventory";

    public Task DetectAsync(HardwareReport report, CancellationToken ct)
    {
        Safe(report, ct, r =>
        {
            var cpu = source.GetCpuList().FirstOrDefault();
            if (cpu is null) return;
            r.Cpu = new CpuInfo(cpu.Name ?? "未知 CPU", cpu.ProcessorId,
                (int)cpu.NumberOfCores, (int)cpu.NumberOfLogicalProcessors,
                (int)cpu.MaxClockSpeed, cpu.L3CacheSize > 0 ? (long)cpu.L3CacheSize : null);
        });
        Safe(report, ct, r =>
        {
            var board = source.GetMotherboardList().FirstOrDefault();
            if (board is not null)
                r.Motherboard = new MotherboardInfo(
                    board.Manufacturer ?? "未知", board.Product ?? "未知", board.SerialNumber);
        });
        Safe(report, ct, r =>
        {
            foreach (var m in source.GetMemoryList())
                r.MemoryModules.Add(new MemoryModule(
                    m.BankLabel ?? "", "", m.Capacity,
                    (int)m.Speed, Clean(m.Manufacturer), Clean(m.PartNumber), Clean(m.SerialNumber), m.FormFactor.ToString()));
        });
        Safe(report, ct, r =>
        {
            foreach (var v in source.GetVideoControllerList())
                r.Gpus.Add(new GpuInfo(v.Name ?? "未知显卡", v.Manufacturer,
                    v.AdapterRAM == 0 ? null : v.AdapterRAM, v.DriverVersion, v.VideoProcessor));
        });
        Safe(report, ct, r =>
        {
            var os = source.GetOperatingSystem();
            if (os is not null)
                r.Os = new OsInfo(
                    os.Name ?? "未知 OS",
                    os.VersionString ?? "",
                    os.Version?.Build.ToString() ?? "",
                    RuntimeInformation.OSArchitecture.ToString());
        });
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static void Safe(HardwareReport r, CancellationToken ct, Action<HardwareReport> section)
    {
        try { ct.ThrowIfCancellationRequested(); section(r); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { r.Errors.Add(new DetectionError("wmi.inventory", ex.Message)); }
    }

    private static string? Clean(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}