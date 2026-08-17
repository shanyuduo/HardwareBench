using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareBench.Core.Detection;
using HardwareBench.Core.Models;
using HardwareBench.Core.Serialization;

namespace HardwareBench.App.ViewModels;

public partial class DetectionViewModel(
    IDetectionService detectionService, IFileSaveService fileSave) : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private HardwareReport? _report;
    [ObservableProperty] private string _statusText = "就绪——点击「开始检测」";

    public bool HasReport => Report is not null;

    partial void OnReportChanged(HardwareReport? value)
        => OnPropertyChanged(nameof(HasReport));

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusText = "检测中……";
        try
        {
            Report = await detectionService.DetectAsync(CancellationToken.None);
            StatusText = Report.Errors.Count == 0
                ? "检测完成，无分项失败"
                : $"检测完成，{Report.Errors.Count} 个分项失败（详见列表）";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task ExportAsync()
    {
        if (Report is null) return Task.CompletedTask;
        var path = fileSave.PickSavePath($"HardwareReport-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        if (path is null) return Task.CompletedTask;
        File.WriteAllText(path, ReportJson.Serialize(Report));
        StatusText = $"已导出：{path}";
        return Task.CompletedTask;
    }
}