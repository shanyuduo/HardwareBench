using System.Globalization;

namespace HardwareBench.Core.Benchmarks.Parsers;

public sealed record SevenZipMetrics(double TotalRatingMips, double CompressRatingMips, double DecompressRatingMips);

public static class SevenZipParser
{
    public static SevenZipMetrics Parse(string stdout)
    {
        double? total = null;
        double? compress = null;
        double? decompress = null;

        foreach (var rawLine in stdout.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("Tot:", StringComparison.Ordinal))
            {
                var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 4)
                    throw new FormatException("7-Zip output: Tot line not found");

                total = double.Parse(tokens[3], CultureInfo.InvariantCulture);
            }
            else if (line.StartsWith("Avr:", StringComparison.Ordinal))
            {
                var parts = line.Split('|');
                if (parts.Length < 2)
                    throw new FormatException("7-Zip output: Avr line not found");

                var left = parts[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                var right = parts[1].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (left.Length < 1 || right.Length < 1)
                    throw new FormatException("7-Zip output: Avr line not found");

                compress = double.Parse(left[^1], CultureInfo.InvariantCulture);
                decompress = double.Parse(right[^1], CultureInfo.InvariantCulture);
            }
        }

        if (total is null)
            throw new FormatException("7-Zip output: Tot line not found");
        if (compress is null || decompress is null)
            throw new FormatException("7-Zip output: Avr line not found");

        return new SevenZipMetrics(total.Value, compress.Value, decompress.Value);
    }
}