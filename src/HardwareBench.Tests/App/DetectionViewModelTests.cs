using HardwareBench.Core.Detection;
using HardwareBench.Core.Models;
using HardwareBench.App.ViewModels;
using HardwareBench.Core.Serialization;

namespace HardwareBench.Tests.App;

public class DetectionViewModelTests
{
    private sealed class FakeService : IDetectionService
    {
        public Task<HardwareReport> DetectAsync(CancellationToken ct) => Task.FromResult(new HardwareReport
        {
            Cpu = new CpuInfo("i7-13700K", null, 16, 24, 5400, null),
            Errors = { new DetectionError("x.y", "boom") }
        });
    }

    private sealed class FakeExporter : IFileSaveService
    {
        public string? PathToReturn { get; set; }
        public string? PickSavePath(string defaultName) => PathToReturn ?? Path.Combine(Path.GetTempPath(), defaultName);
    }

    [Fact]
    public async Task Load_PopulatesReportAndStatus()
    {
        var vm = new DetectionViewModel(new FakeService(), new FakeExporter());

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.IsBusy);
        Assert.NotNull(vm.Report);
        Assert.Equal("i7-13700K", vm.Report!.Cpu!.Name);
        Assert.Contains("1", vm.StatusText);
    }

    [Fact]
    public async Task Export_WritesJsonToChosenPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hb-test-{Guid.NewGuid():N}.json");
        var exporter = new FakeExporter { PathToReturn = path };
        var vm = new DetectionViewModel(new FakeService(), exporter);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.True(File.Exists(path));
        var json = File.ReadAllText(path);
        Assert.Contains("i7-13700K", json);
        File.Delete(path);
    }
}