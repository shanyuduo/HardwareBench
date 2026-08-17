using HardwareBench.Core.Detection.Edid;

namespace HardwareBench.Tests.Detection;

public class EdidParserTests
{
    private static byte[] BuildEdid(Action<byte[]> mutate)
    {
        var b = new byte[128];
        b[0] = 0x00; b[1] = 0xFF; b[2] = 0xFF; b[3] = 0xFF;
        b[4] = 0xFF; b[5] = 0xFF; b[6] = 0xFF; b[7] = 0x00;
        b[8] = 0x21; b[9] = 0x4C;
        b[10] = 0xFF; b[11] = 0x40;
        b[12] = 0x78; b[13] = 0x56; b[14] = 0x34; b[15] = 0x12;
        b[16] = 23;
        b[17] = 36;
        b[18] = 1; b[19] = 4;
        b[72] = 0; b[73] = 0; b[74] = 0; b[75] = 0xFC;
        var name = "U2415F"u8;
        name.CopyTo(b.AsSpan(77));
        b[90] = 0; b[91] = 0; b[92] = 0; b[93] = 0xFF;
        var sn = "CN0123456789"u8;
        sn.CopyTo(b.AsSpan(95));
        int sum = 0;
        for (int i = 0; i < 127; i++) sum += b[i];
        b[127] = (byte)((256 - (sum % 256)) % 256);
        mutate(b);
        return b;
    }

    [Fact]
    public void Parse_ReadsAllPlantedFields()
    {
        var edid = BuildEdid(_ => { });

        var d = EdidParser.Parse(edid);

        Assert.Equal("DEL", d.ManufacturerId);
        Assert.Equal(0x40FF, d.ProductCode);
        Assert.Equal(0x12345678u, d.SerialNumber);
        Assert.Equal(23, d.MadeWeek);
        Assert.Equal(2026, d.MadeYear);
        Assert.Equal(1, d.VersionMajor);
        Assert.Equal(4, d.VersionMinor);
        Assert.Equal("U2415F", d.ModelName);
        Assert.Equal("CN0123456789", d.SerialString);
        Assert.True(d.ChecksumValid);
    }

    [Fact]
    public void Parse_CorruptChecksum_ReportsInvalidButParses()
    {
        var edid = BuildEdid(b => b[40] ^= 0xFF);

        var d = EdidParser.Parse(edid);

        Assert.False(d.ChecksumValid);
        Assert.Equal("DEL", d.ManufacturerId);
    }

    [Fact]
    public void Parse_BadHeader_Throws()
    {
        var edid = BuildEdid(b => b[1] = 0x00);

        Assert.Throws<FormatException>(() => EdidParser.Parse(edid));
    }

    [Fact]
    public void Parse_TooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => EdidParser.Parse(new byte[127]));
    }
}