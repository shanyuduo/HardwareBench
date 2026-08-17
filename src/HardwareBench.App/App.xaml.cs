using System.Windows;
using HardwareBench.App.ViewModels;
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
        b.Services.AddSingleton(sp =>
        {
            var detection = sp.GetRequiredService<DetectionViewModel>();
            var benchmark = new PlaceholderViewModel("性能跑分将在 M2 里程碑启用");
            var history = new PlaceholderViewModel("历史结果将在 M3 里程碑启用");
            return new MainViewModel(detection, benchmark, history);
        });
        b.Services.AddSingleton<MainWindow>();
        _host = b.Build();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
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