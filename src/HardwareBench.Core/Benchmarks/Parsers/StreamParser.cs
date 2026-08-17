using System.Globalization;

namespace HardwareBench.Core.Benchmarks.Parsers;

public sealed record StreamMetrics(double Copy, double Scale, double Add, double Triad);

public static class StreamParser
{
    public static StreamMetrics Parse(string stdout)
    {
        double? copy = null;
        double? scale = null;
        double? add = null;
        double? triad = null;

        foreach (var rawLine in stdout.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("Copy:", StringComparison.Ordinal))
                copy = ParseRate(line, "Copy");
            else if (line.StartsWith("Scale:", StringComparison.Ordinal))
                scale = ParseRate(line, "Scale");
            else if (line.StartsWith("Add:", StringComparison.Ordinal))
                add = ParseRate(line, "Add");
            else if (line.StartsWith("Triad:", StringComparison.Ordinal))
                triad = ParseRate(line, "Triad");
        }

        if (copy is null)
            throw new FormatException("STREAM output: Copy line not found");
        if (scale is null)
            throw new FormatException("STREAM output: Scale line not found");
        if (add is null)
            throw new FormatException("STREAM output: Add line not found");
        if (triad is null)
            throw new FormatException("STREAM output: Triad line not found");

        return new StreamMetrics(copy.Value, scale.Value, add.Value, triad.Value);
    }

    private static double ParseRate(string line, string name)
    {
        var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
            throw new FormatException($"STREAM output: {name} line not found");

        return double.Parse(tokens[1], CultureInfo.InvariantCulture);
    }
}