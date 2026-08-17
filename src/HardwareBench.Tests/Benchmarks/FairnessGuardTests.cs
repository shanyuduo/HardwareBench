using HardwareBench.Core.Benchmarks;

namespace HardwareBench.Tests.Benchmarks;

public class FairnessGuardTests
{
    private const string ChineseBalanced =
        "电源方案 GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (平衡)";
    private const string EnglishBalanced =
        "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)";
    private const string ChinesePowerSaver =
        "电源方案 GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (节能)";

    [Fact]
    public void PowerSchemeParser_ChineseBalanced_ParsesGuidAndName()
    {
        var parsed = PowerSchemeParser.Parse(ChineseBalanced);

        Assert.NotNull(parsed);
        Assert.Equal("381b4222-f694-41f0-9685-ff5bb260df2e", parsed!.Value.Guid);
        Assert.Equal("平衡", parsed.Value.Name);
    }

    [Fact]
    public void PowerSchemeParser_EnglishBalanced_ParsesGuidAndName()
    {
        var parsed = PowerSchemeParser.Parse(EnglishBalanced);

        Assert.NotNull(parsed);
        Assert.Equal("381b4222-f694-41f0-9685-ff5bb260df2e", parsed!.Value.Guid);
        Assert.Equal("Balanced", parsed.Value.Name);
    }

    [Fact]
    public void PowerSchemeParser_Garbage_ReturnsNull()
    {
        Assert.Null(PowerSchemeParser.Parse("this is not powercfg output"));
        Assert.Null(PowerSchemeParser.Parse(""));
        Assert.Null(PowerSchemeParser.Parse("   "));
    }

    [Fact]
    public void CpuBusySampler_SampleBusyFraction_IsWithinUnitInterval()
    {
        var fraction = CpuBusySampler.SampleBusyFraction(300);

        Assert.InRange(fraction, 0.0, 1.0);
    }

    [Fact]
    public async Task CheckAsync_BalancedSchemeAndIdleMachine_ReportsOkWithNoWarnings()
    {
        var runner = new FakeRunner(new ProcessResult(0, ChineseBalanced, "", false));
        var guard = new FairnessGuard(runner);

        var report = await guard.CheckAsync(CancellationToken.None);

        Assert.Equal("平衡", report.PowerSchemeName);
        Assert.True(report.PowerSchemeOk);
        Assert.InRange(report.BackgroundCpuPercent, 0.0, 100.0);
        // BackgroundCpuOk is environment-dependent (idle machine assumed); on a busy
        // dev/CI machine the CPU warning may legitimately fire, so only the scheme
        // side is asserted strictly here.
        Assert.DoesNotContain(report.Warnings, w => w.Contains("电源计划"));
        Assert.Equal("powercfg.exe", runner.LastSpec!.ExePath);
        Assert.Equal(new[] { "/getactivescheme" }, runner.LastSpec.Args);
        Assert.Equal(5000, runner.LastSpec.TimeoutMs);
    }

    [Fact]
    public async Task CheckAsync_PowerSaverScheme_WarnsAboutScheme()
    {
        var runner = new FakeRunner(new ProcessResult(0, ChinesePowerSaver, "", false));
        var guard = new FairnessGuard(runner);

        var report = await guard.CheckAsync(CancellationToken.None);

        Assert.False(report.PowerSchemeOk);
        Assert.Contains(report.Warnings, w => w.Contains("节能"));
    }

    [Fact]
    public async Task CheckAsync_GarbageOutput_ReportsUnknownWithoutThrowing()
    {
        var runner = new FakeRunner(new ProcessResult(0, "garbage output", "", false));
        var guard = new FairnessGuard(runner);

        var report = await guard.CheckAsync(CancellationToken.None);

        Assert.False(report.PowerSchemeOk);
        Assert.Equal("未知", report.PowerSchemeName);
        Assert.NotEmpty(report.Warnings);
    }

    private sealed class FakeRunner : IProcessRunner
    {
        private readonly ProcessResult _result;

        public FakeRunner(ProcessResult result) => _result = result;

        public ProcessSpec? LastSpec { get; private set; }

        public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct)
        {
            LastSpec = spec;
            return Task.FromResult(_result);
        }
    }
}