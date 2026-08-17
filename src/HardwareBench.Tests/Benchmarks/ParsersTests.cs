using HardwareBench.Core.Benchmarks.Parsers;

namespace HardwareBench.Tests.Benchmarks;

public class ParsersTests
{
    public class DiskSpdParserTests
    {
        private const string Fixture = """
            Total IO
            thread |       bytes     |     I/Os     |    MiB/s   |  I/O per s |  file
            ------------------------------------------------------------------------------
                 0 |      3099066368 |        47288 |     589.64 |    9434.21 | testfile.dat (50MiB)
            ------------------------------------------------------------------------------
            total:        3099066368 |        47288 |     589.64 |    9434.21

            Read IO
            thread |       bytes     |     I/Os     |    MiB/s   |  I/O per s |  file
            ------------------------------------------------------------------------------
                 0 |      493993984 |       120604 |     156.94 |   40177.44 | testfile.dat (50MiB)
            ------------------------------------------------------------------------------
            total:           493993984 |       120604 |     156.94 |   40177.44
            """;

        [Fact]
        public void Parse_ValidFixture_ReturnsReadIoMetrics()
        {
            var m = DiskSpdParser.Parse(Fixture);

            Assert.Equal(156.94, m.ReadMiBS);
            Assert.Equal(40177.44, m.ReadIops);
        }

        [Fact]
        public void Parse_MissingReadIoSection_ThrowsFormatException()
        {
            var ex = Assert.Throws<FormatException>(() => DiskSpdParser.Parse(Fixture.Split("Read IO")[0]));

            Assert.Equal("DiskSpd output: Read IO total line not found", ex.Message);
        }
    }

    public class SevenZipParserTests
    {
        private const string Fixture = """
            Dict     Speed Usage    R/U Rating  |      Speed Usage    R/U Rating
                     KiB/s     %   MIPS   MIPS  |      KiB/s     %   MIPS   MIPS

            22:      71186  1340   5168  69250  |     631077  1822   2953  53806
            Avr:     66360  1333   5079  67701  |     626231  1859   2913  54160
            Tot:            1596   3996  60931
            """;

        [Fact]
        public void Parse_ValidFixture_ReturnsRatings()
        {
            var m = SevenZipParser.Parse(Fixture);

            Assert.Equal(60931, m.TotalRatingMips);
            Assert.Equal(67701, m.CompressRatingMips);
            Assert.Equal(54160, m.DecompressRatingMips);
        }

        [Fact]
        public void Parse_MissingTotLine_ThrowsFormatException()
        {
            var ex = Assert.Throws<FormatException>(() => SevenZipParser.Parse(Fixture.Replace("Tot:", "TotX:")));

            Assert.Equal("7-Zip output: Tot line not found", ex.Message);
        }
    }

    public class StreamParserTests
    {
        private const string Fixture = """
            Function    Best Rate MB/s  Avg time     Min time     Max time
            Copy:           33085.9     0.005104     0.004836     0.006060
            Scale:          24600.2     0.007200     0.006504     0.008428
            Add:            28864.5     0.009421     0.008315     0.011756
            Triad:          27896.9     0.009346     0.008603     0.011676
            """;

        [Fact]
        public void Parse_ValidFixture_ReturnsRates()
        {
            var m = StreamParser.Parse(Fixture);

            Assert.Equal(33085.9, m.Copy);
            Assert.Equal(24600.2, m.Scale);
            Assert.Equal(28864.5, m.Add);
            Assert.Equal(27896.9, m.Triad);
        }

        [Fact]
        public void Parse_MissingTriadLine_ThrowsFormatException()
        {
            var ex = Assert.Throws<FormatException>(() => StreamParser.Parse(Fixture.Replace("Triad:", "TriadX:")));

            Assert.Equal("STREAM output: Triad line not found", ex.Message);
        }
    }
}