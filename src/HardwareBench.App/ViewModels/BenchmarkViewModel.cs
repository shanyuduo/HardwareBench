using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareBench.Core.Benchmarks;

namespace HardwareBench.App.ViewModels;

public partial class BenchmarkViewModel(
    IBenchmarkService service, IFileSaveService fileSave) : ObservableObject
{
    public record MetricRow(string Id, string RawDisplay, string ScoreDisplay);

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _progressText = "就绪——点击「开始跑分」";
    [ObservableProperty] private bool _indeterminate;
    [ObservableProperty] private string _fairnessText = "";
    [ObservableProperty] private ObservableCollection<MetricRow> _metricRows = [];
    [ObservableProperty] private string _totalText = "";
    [ObservableProperty] private bool _hasResults;

    private BenchmarkResult? _lastResult;

    [RelayCommand]
    private async Task StartAsync()
    {
        MetricRows.Clear();
        HasResults = false;
        _lastResult = null;
        IsBusy = true;
        Indeterminate = true;
        ProgressText = "跑分中……";
        try
        {
            var progress = new Progress<BenchmarkProgress>(p =>
                ProgressText = $"引擎 {p.EngineId} 第 {p.RunIndex}/{p.TotalRuns} 轮（{(p.Phase == "warmup" ? "预热" : "正式")}）");

            var result = await service.RunAsync(progress, CancellationToken.None);
            _lastResult = result;

            FairnessText = BuildFairnessText(result);
            foreach (var m in result.Metrics)
            {
                MetricRows.Add(new MetricRow(
                    m.Id,
                    $"{m.Value:F1} {m.Unit}",
                    m.Score.HasValue ? $"{m.Score} / 1000" : "—"));
            }

            TotalText = result.TotalScore is { } score
                ? $"总分 {score}（几何平均，参照表 {ReferenceTable.Version}）"
                : "总分 —";
            HasResults = true;
            ProgressText = "跑分完成";
        }
        catch (OperationCanceledException)
        {
            ProgressText = "跑分已取消";
        }
        catch (Exception ex)
        {
            ProgressText = $"跑分失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
            Indeterminate = false;
        }
    }

    [RelayCommand]
    private Task ExportAsync()
    {
        if (!HasResults || _lastResult is null)
            return Task.CompletedTask;

        var path = fileSave.PickSavePath($"HardwareBench-result-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        if (path is null)
            return Task.CompletedTask;

        File.WriteAllText(path, BenchmarkJson.Serialize(_lastResult));
        ProgressText = $"已导出：{path}";
        return Task.CompletedTask;
    }

    private static string BuildFairnessText(BenchmarkResult result)
    {
        var warns = result.Environment.Warnings;
        return warns is { Count: > 0 } ? string.Join("；", warns) : "环境检查通过";
    }
}