namespace HardwareBench.Core.Detection.Edid;

public static class EdidParser
{
    private static ReadOnlySpan<byte> Header => [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];

    public static EdidData Parse(ReadOnlySpan<byte> edid)
    {
        if (edid.Length < 128)
            throw new ArgumentException("EDID 至少需要 128 字节基块。", nameof(edid));
        if (!edid[..8].SequenceEqual(Header))
            throw new FormatException("Invalid EDID header");

        string mfg = DecodeManufacturerId(edid[8], edid[9]);
        ushort product = (ushort)(edid[10] | (edid[11] << 8));
        uint serial = (uint)(edid[12] | (edid[13] << 8) | (edid[14] << 16) | (edid[15] << 24));
        byte week = edid[16];
        ushort year = (ushort)(1990 + edid[17]);
        int sum = 0;
        for (int i = 0; i < 128; i++) sum += edid[i];
        bool checksumValid = sum % 256 == 0;

        string? modelName = null, serialString = null;
        for (int block = 0; block < 4; block++)
        {
            int off = 54 + block * 18;
            if (edid[off] != 0 || edid[off + 1] != 0 || edid[off + 2] != 0) continue;
            switch (edid[off + 3])
            {
                case 0xFC: modelName = ReadString(edid[(off + 5)..(off + 18)]); break;
                case 0xFF: serialString = ReadString(edid[(off + 5)..(off + 18)]); break;
            }
        }

        return new EdidData(mfg, product, serial, week, year,
            edid[18], edid[19], modelName, serialString, edid[126], checksumValid);
    }

    private static string DecodeManufacturerId(byte hi, byte lo)
    {
        char c1 = (char)('A' - 1 + ((hi >> 3) & 0x1F));
        char c2 = (char)('A' - 1 + (((hi & 0x07) << 2) | ((lo >> 6) & 0x03)));
        char c3 = (char)('A' - 1 + (lo & 0x1F));
        return new string([c1, c2, c3]);
    }

    private static string? ReadString(ReadOnlySpan<byte> span)
    {
        int end = span.IndexOf((byte)0x0A);
        if (end < 0) end = span.IndexOf((byte)0);
        if (end < 0) end = span.Length;
        var s = System.Text.Encoding.ASCII.GetString(span[..end]).TrimEnd('\n', '\r', ' ');
        return s.Length == 0 ? null : s;
    }
}