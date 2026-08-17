using HardwareBench.Core.Detection.Edid;
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection.Monitor;

public sealed class MonitorDetector(IEdidSource source)
{
    public string Id => "monitor.edid";

    public Task DetectAsync(HardwareReport report, CancellationToken ct)
    {
        foreach (var (instancePath, edid) in source.ReadAll())
        {
            ct.ThrowIfCancellationRequested();
            EdidData d;
            try { d = EdidParser.Parse(edid); }
            catch (FormatException) { continue; }
            report.Monitors.Add(new MonitorInfo(
                instancePath, d.ManufacturerId, d.ProductCode,
                d.ModelName, d.SerialString, d.MadeYear, d.MadeWeek));
        }
        return Task.CompletedTask;
    }
}