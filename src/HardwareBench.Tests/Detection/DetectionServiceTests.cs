using HardwareBench.Core.Detection;
using HardwareBench.Core.Models;

namespace HardwareBench.Tests.Detection;

public class DetectionServiceTests
{
    private sealed class FakeDetector : IHardwareDetector
    {
        public string Id { get; }
        private readonly Action<HardwareReport> _act;
        public FakeDetector(string id, Action<HardwareReport> act) { Id = id; _act = act; }
        public Task DetectAsync(HardwareReport report, CancellationToken ct) { _act(report); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Detect_RunsAll_FaultTolerant_AndStampsTime()
    {
        var before = DateTimeOffset.UtcNow;
        var service = new DetectionService(new IHardwareDetector[]
        {
            new FakeDetector("cpu.ok", r => r.Cpu = new CpuInfo("i7", null, 8, 16, 5000, null)),
            new FakeDetector("bad.one", _ => throw new IOException("boom")),
            new FakeDetector("gpu.ok", r => r.Gpus.Add(new GpuInfo("RTX", null, null, null, null)))
        });

        var report = await service.DetectAsync(CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal("i7", report.Cpu!.Name);
        Assert.Single(report.Gpus);
        var err = Assert.Single(report.Errors);
        Assert.Equal("bad.one", err.DetectorId);
        Assert.Equal("boom", err.Message);
        Assert.InRange(report.CapturedAtUtc, before, after);
    }

    [Fact]
    public async Task Detect_Cancellation_Propagates()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var service = new DetectionService(
            [new FakeDetector("x", _ => { })]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DetectAsync(cts.Token));
    }
}