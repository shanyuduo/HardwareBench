using HardwareBench.Core.Benchmarks;

namespace HardwareBench.Tests.Benchmarks;

public class ScoringTests
{
    public class ComputeScoreTests
    {
        [Fact]
        public void ZeroValue_ReturnsZero()
        {
            Assert.Equal(0, Scoring.ComputeScore(0, 7000));
        }

        [Fact]
        public void ValueEqualToRef_Returns1000()
        {
            Assert.Equal(1000, Scoring.ComputeScore(7000, 7000));
        }

        [Fact]
        public void ValueAboveRef_CapsAt1000()
        {
            Assert.Equal(1000, Scoring.ComputeScore(2 * 7000, 7000));
            Assert.Equal(1000, Scoring.ComputeScore(1.5 * 7000, 7000));
        }

        [Fact]
        public void HalfRef_Returns500()
        {
            Assert.Equal(500, Scoring.ComputeScore(3500, 7000));
        }

        [Fact]
        public void NegativeValue_ReturnsZero()
        {
            Assert.Equal(0, Scoring.ComputeScore(-100, 7000));
        }

        [Fact]
        public void NaNInput_ReturnsZero()
        {
            Assert.Equal(0, Scoring.ComputeScore(double.NaN, 7000));
            Assert.Equal(0, Scoring.ComputeScore(7000, double.NaN));
        }

        [Fact]
        public void MidpointRoundsAwayFromZero()
        {
            // 1/16*1000 = 62.5 exactly -> 63 (AwayFromZero; banker's ToEven would give 62)
            Assert.Equal(63, Scoring.ComputeScore(1, 16));
        }
    }

    public class GeometricMeanTests
    {
        [Fact]
        public void Empty_ReturnsZero()
        {
            Assert.Equal(0, Scoring.GeometricMean([]));
        }

        [Fact]
        public void AllEqual_ReturnsSame()
        {
            Assert.Equal(1000, Scoring.GeometricMean([1000, 1000]));
        }

        [Fact]
        public void AnyNonPositive_ReturnsZero()
        {
            Assert.Equal(0, Scoring.GeometricMean([1000, 0]));
            Assert.Equal(0, Scoring.GeometricMean([1000, -5]));
        }

        [Fact]
        public void MixedValues_ReturnsRoundedGeometricMean()
        {
            // exp((ln100 + ln225 + ln1000)/3) = exp(5.64301) ≈ 282.31 -> 282
            Assert.Equal(282, Scoring.GeometricMean([100, 225, 1000]));
        }
    }

    public class MedianTests
    {
        [Fact]
        public void OddCount_ReturnsMiddleOfOrderedList()
        {
            Assert.Equal(5, Scoring.Median([1, 9, 5]));
        }

        [Fact]
        public void EvenCount_ReturnsAverageOfTwoMiddles()
        {
            Assert.Equal(2.5, Scoring.Median([1, 2, 3, 4]));
        }

        [Fact]
        public void Empty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Scoring.Median([]));
        }
    }
}

public class ReferenceTableTests
{
    [Theory]
    [InlineData("disk-seq-read", "MiB/s", 7000)]
    [InlineData("disk-4k-read", "MiB/s", 400)]
    [InlineData("cpu-7z-rating", "MIPS", 100000)]
    [InlineData("mem-triad", "MB/s", 80000)]
    public void KnownIds_ReturnExactEntry(string id, string unit, double ref1000)
    {
        Assert.True(ReferenceTable.TryGet(id, out var entry));
        Assert.Equal(unit, entry.Unit);
        Assert.Equal(ref1000, entry.Ref1000);
    }

    [Fact]
    public void UnknownId_ReturnsFalse()
    {
        Assert.False(ReferenceTable.TryGet("nope", out var entry));
        Assert.Equal(default, entry);
    }

    [Fact]
    public void Version_Is2026_08_m2()
    {
        Assert.Equal("2026.08-m2", ReferenceTable.Version);
    }
}