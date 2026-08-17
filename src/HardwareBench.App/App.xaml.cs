using System.IO;
using System.Windows;
using HardwareBench.App.ViewModels;
using HardwareBench.Core.Benchmarks;
using HardwareBench.Core.Benchmarks.Engines;
using HardwareBench.Core.Detection;
using HardwareBench.Core.Detection.Monitor;
using HardwareBench.Core.Detection.Peripheral;
using HardwareBench.Core.Detection.Storage;
using HardwareBench.Core.Detection.Wmi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace HardwareBench.App;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        var b = Host.CreateApplicationBuilder();
        b.Services.AddSingleton<IWmiSource, HardwareInfoWmiSource>();
        b.Services.AddSingleton<IEdidSource, RegistryEdidSource>();
        b.Services.AddSingleton<NativeStorageApi>();
        b.Services.AddSingleton<RegistryPeripheralSource>();
        b.Services.AddSingleton<IHardwareDetector, WmiInventoryDetector>();
        b.Services.AddSingleton<IHardwareDetector, StorageDetector>();
        b.Services.AddSingleton<IHardwareDetector, MonitorDetector>();
        b.Services.AddSingleton<IHardwareDetector, PeripheralRegistryDetector>();
        b.Services.AddSingleton<IDetectionService, DetectionService>();
        b.Services.AddSingleton<IFileSaveService, DialogFileSaveService>();
        b.Services.AddSingleton<DetectionViewModel>();
        b.Services.AddSingleton<IToolBinariesSource, AssemblyResourceBinariesSource>();
        b.Services.AddSingleton<IToolLocator>(sp => new ToolExtractor(sp.GetRequiredService<IToolBinariesSource>()));
        b.Services.AddSingleton<IProcessRunner, BoundedProcessRunner>();
        b.Services.AddSingleton<IFairnessGuard, FairnessGuard>();
        b.Services.AddSingleton<IBenchmarkEngine, DiskSpdEngine>();
        b.Services.AddSingleton<IBenchmarkEngine, SevenZipEngine>();
        b.Services.AddSingleton<IBenchmarkEngine, MemoryStreamEngine>();
        b.Services.AddSingleton<IBenchmarkService, BenchmarkService>();
        b.Services.AddSingleton<BenchmarkViewModel>();
        b.Services.AddSingleton(sp =>
        {
            var detection = sp.GetRequiredService<DetectionViewModel>();
            var benchmark = sp.GetRequiredService<BenchmarkViewModel>();
            var history = new PlaceholderViewModel("历史结果将在 M3 里程碑启用");
            return new MainViewModel(detection, benchmark, history);
        });
        b.Services.AddSingleton<MainWindow>();
        _host = b.Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Contains("--benchmark"))
        {
            _ = RunHeadlessAsync();
            return;
        }

        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);

        async Task RunHeadlessAsync()
        {
            var exportPath = GetExportPath(e.Args);
            try
            {
                var service = _host.Services.GetRequiredService<IBenchmarkService>();
                var result = await service.RunAsync(null, CancellationToken.None);
                File.WriteAllText(exportPath, BenchmarkJson.Serialize(result));
                Shutdown(0);
            }
            catch (Exception ex)
            {
                try
                {
                    var errorPath = Path.Combine(
                        Path.GetDirectoryName(exportPath) ?? ".",
                        Path.GetFileNameWithoutExtension(exportPath) + ".error.txt");
                    File.WriteAllText(errorPath, ex.ToString());
                }
                catch
                {
                    // Console may be unavailable in a WinExe; ignore.
                }

                Shutdown(1);
            }
        }
    }

    private static string GetExportPath(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--export")
                return args[i + 1];
        }

        return Path.Combine(Environment.CurrentDirectory, $"HardwareBench-result-{DateTime.Now:yyyyMMdd-HHmmss}.json");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.Dispose();
        base.OnExit(e);
    }
}

public sealed class DialogFileSaveService : IFileSaveService
{
    public string? PickSavePath(string defaultName)
    {
        var dlg = new SaveFileDialog { Filter = "JSON 文件|*.json", FileName = defaultName };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}