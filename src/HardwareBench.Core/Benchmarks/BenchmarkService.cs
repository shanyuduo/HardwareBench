using HardwareBench.Core.Benchmarks.Engines;

namespace HardwareBench.Core.Benchmarks;

public sealed record BenchmarkProgress(string EngineId, string Phase, int RunIndex, int TotalRuns);

public interface IBenchmarkService
{
    Task<BenchmarkResult> RunAsync(IProgress<BenchmarkProgress>? progress, CancellationToken ct);
}

public sealed class BenchmarkService(IEnumerable<IBenchmarkEngine> engines, IFairnessGuard guard) : IBenchmarkService
{
    private const int FormalRuns = 3;

    public async Task<BenchmarkResult> RunAsync(IProgress<BenchmarkProgress>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var result = new BenchmarkResult { StartedAtUtc = DateTimeOffset.UtcNow };

        try
        {
            var report = await guard.CheckAsync(ct);
            result.Environment = new BenchmarkEnvironment(
                report.PowerSchemeName, report.BackgroundCpuPercent, ToolHashes.ToolVersions, report.Warnings);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new BenchmarkError("fairness.guard", ex.Message));
            result.Environment = new BenchmarkEnvironment("未知", -1, ToolHashes.ToolVersions, [$"公平性检查失败：{ex.Message}"]);
        }

        foreach (var engine in engines)
        {
            await RunEngineAsync(engine, result, progress, ct);
        }

        var scores = result.Metrics.Where(m => m.Score.HasValue).Select(m => m.Score!.Value).ToList();
        result.TotalScore = scores.Count == 0 ? null : Scoring.GeometricMean(scores);

        return result;
    }

    private static async Task RunEngineAsync(
        IBenchmarkEngine engine, BenchmarkResult result, IProgress<BenchmarkProgress>? progress, CancellationToken ct)
    {
        try
        {
            for (int i = 1; i <= engine.WarmupRuns; i++)
            {
                progress?.Report(new BenchmarkProgress(engine.Id, "warmup", i, engine.WarmupRuns));
                await engine.RunOnceAsync(ct);
            }

            var rounds = new List<IReadOnlyList<MetricValue>>(FormalRuns);
            for (int i = 1; i <= FormalRuns; i++)
            {
                progress?.Report(new BenchmarkProgress(engine.Id, "run", i, FormalRuns));
                rounds.Add(await engine.RunOnceAsync(ct));
            }

            if (!TryBuildMetrics(engine, rounds, out var metrics, out var faultMessage))
            {
                result.Errors.Add(new BenchmarkError(engine.Id, faultMessage));
                return;
            }

            result.Metrics.AddRange(metrics);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new BenchmarkError(engine.Id, ex.Message));
        }
    }

    private static bool TryBuildMetrics(
        IBenchmarkEngine engine,
        IReadOnlyList<IReadOnlyList<MetricValue>> rounds,
        out List<MetricResult> metrics,
        out string faultMessage)
    {
        metrics = [];
        faultMessage = "";

        var ids = rounds[0].Select(m => m.Id).ToList();
        foreach (var round in rounds)
        {
            if (round.Count != ids.Count || !round.Select(m => m.Id).SequenceEqual(ids))
            {
                faultMessage = "engine returned inconsistent metric sets across runs";
                return false;
            }
        }

        foreach (var id in ids)
        {
            var values = rounds.Select(r => r.First(m => m.Id == id).Value).ToList();
            var unit = rounds[0].First(m => m.Id == id).Unit;
            double median = Scoring.Median(values);
            int? score = ReferenceTable.TryGet(id, out var entry)
                ? Scoring.ComputeScore(median, entry.Ref1000)
                : null;
            metrics.Add(new MetricResult(id, engine.Category, unit, median, score, values.ToArray()));
        }

        return true;
    }
}