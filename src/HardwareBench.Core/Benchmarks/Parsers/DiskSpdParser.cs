using System.Globalization;

namespace HardwareBench.Core.Benchmarks.Parsers;

public sealed record DiskSpdMetrics(double ReadMiBS, double ReadIops);

public static class DiskSpdParser
{
    public static DiskSpdMetrics Parse(string stdout)
    {
        var lines = stdout.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() != "Read IO")
                continue;

            for (var j = i + 1; j < lines.Length; j++)
            {
                var trimmed = lines[j].Trim();
                if (!trimmed.StartsWith("total:", StringComparison.Ordinal))
                    continue;

                var cols = trimmed.Split('|');
                if (cols.Length < 4)
                    throw new FormatException("DiskSpd output: Read IO total line not found");

                return new DiskSpdMetrics(
                    double.Parse(cols[2], CultureInfo.InvariantCulture),
                    double.Parse(cols[3], CultureInfo.InvariantCulture));
            }
        }

        throw new FormatException("DiskSpd output: Read IO total line not found");
    }
}