using System.Runtime.Versioning;
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection.Peripheral;

[SupportedOSPlatform("windows")]
public sealed class PeripheralRegistryDetector(RegistryPeripheralSource source) : IHardwareDetector
{
    public string Id => "peripheral.registry";

    public Task DetectAsync(HardwareReport report, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        report.Peripherals.AddRange(PeripheralParser.Parse(source.ReadAll()));
        return Task.CompletedTask;
    }
}