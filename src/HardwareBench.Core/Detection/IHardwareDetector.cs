using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection;

public interface IHardwareDetector
{
    string Id { get; }
    Task DetectAsync(HardwareReport report, CancellationToken ct);
}

public interface IDetectionService
{
    Task<HardwareReport> DetectAsync(CancellationToken ct);
}