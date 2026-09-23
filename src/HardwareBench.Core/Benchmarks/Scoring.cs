namespace HardwareBench.Core.Benchmarks;

public static class Scoring
{
    public static int ComputeScore(double value, double ref1000)
    {
        if (double.IsNaN(value) || double.IsNaN(ref1000))
        {
            return 0;
        }

        double raw = value / ref1000 * 1000;
        int rounded = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, 1000);
    }

    public static int GeometricMean(IReadOnlyList<int> scores)
    {
        if (scores.Count == 0)
        {
            return 0;
        }

        double sum = 0;
        foreach (int score in scores)
        {
            if (score <= 0)
            {
                return 0;
            }

            sum += Math.Log(score);
        }

        double mean = sum / scores.Count;
        return (int)Math.Round(Math.Exp(mean), MidpointRounding.AwayFromZero);
    }

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("Median requires at least one value.", nameof(values));
        }

        var ordered = values.OrderBy(v => v).ToArray();
        int mid = ordered.Length / 2;
        if (ordered.Length % 2 == 1)
        {
            return ordered[mid];
        }

        return (ordered[mid - 1] + ordered[mid]) / 2.0;
    }
}