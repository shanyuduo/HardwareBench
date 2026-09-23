namespace HardwareBench.Core.Benchmarks;

public static class ReferenceTable
{
    public const string Version = "2026.08-m2";

    private static readonly Dictionary<string, (string Unit, double Ref1000)> Entries = new()
    {
        ["disk-seq-read"] = ("MiB/s", 7000),
        ["disk-4k-read"] = ("MiB/s", 400),
        ["cpu-7z-rating"] = ("MIPS", 100000),
        ["mem-triad"] = ("MB/s", 80000),
    };

    public static bool TryGet(string metricId, out (string Unit, double Ref1000) entry)
    {
        return Entries.TryGetValue(metricId, out entry);
    }
}