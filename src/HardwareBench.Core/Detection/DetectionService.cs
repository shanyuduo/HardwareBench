using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection;

public sealed class DetectionService(IEnumerable<IHardwareDetector> detectors) : IDetectionService
{
    public async Task<HardwareReport> DetectAsync(CancellationToken ct)
    {
        var report = new HardwareReport { CapturedAtUtc = DateTimeOffset.UtcNow };
        foreach (var detector in detectors)
        {
            ct.ThrowIfCancellationRequested();
            try { await detector.DetectAsync(report, ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { report.Errors.Add(new DetectionError(detector.Id, ex.Message)); }
        }
        return report;
    }
}