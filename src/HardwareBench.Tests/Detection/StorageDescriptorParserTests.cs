using HardwareBench.Core.Detection.Storage;

namespace HardwareBench.Tests.Detection;

public class StorageDescriptorParserTests
{
    private static byte[] BuildBuffer()
    {
        var b = new byte[128];
        WriteU32(b, 0, 36 + 11 + 17 + 4 + 1 + 13);
        WriteU32(b, 12, 36);
        WriteU32(b, 16, 47);
        WriteU32(b, 20, 63);
        WriteU32(b, 24, 68);
        WriteU32(b, 28, 17);
        System.Text.Encoding.ASCII.GetBytes("Samsung ").CopyTo(b, 36);
        System.Text.Encoding.ASCII.GetBytes("SSD 990 PRO 2TB").CopyTo(b, 47);
        System.Text.Encoding.ASCII.GetBytes("1B2Q").CopyTo(b, 63);
        System.Text.Encoding.ASCII.GetBytes("S6Z1NJ0R12345").CopyTo(b, 68);
        return b;
    }

    private static void WriteU32(byte[] b, int off, uint v)
    {
        b[off] = (byte)v; b[off + 1] = (byte)(v >> 8);
        b[off + 2] = (byte)(v >> 16); b[off + 3] = (byte)(v >> 24);
    }

    [Fact]
    public void Parse_ReadsStringsAndBusType()
    {
        var d = StorageDescriptorParser.Parse(BuildBuffer());

        Assert.Equal("Samsung", d.Vendor);
        Assert.Equal("SSD 990 PRO 2TB", d.Product);
        Assert.Equal("1B2Q", d.Revision);
        Assert.Equal("S6Z1NJ0R12345", d.Serial);
        Assert.Equal(17u, d.BusType);
        Assert.Null(d.TemperatureC);
    }

    [Fact]
    public void Parse_ZeroOffsets_ReturnNulls()
    {
        var b = BuildBuffer();
        WriteU32(b, 12, 0); WriteU32(b, 16, 0); WriteU32(b, 20, 0); WriteU32(b, 24, 0);

        var d = StorageDescriptorParser.Parse(b);

        Assert.Null(d.Vendor); Assert.Null(d.Product);
        Assert.Null(d.Revision); Assert.Null(d.Serial);
    }

    [Fact]
    public void Parse_TrimsTrailingSpaces()
    {
        var b = BuildBuffer();
        WriteU32(b, 16, 0);
        var d = StorageDescriptorParser.Parse(b);
        Assert.Equal("Samsung", d.Vendor);
    }
}