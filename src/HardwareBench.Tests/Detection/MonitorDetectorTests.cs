using HardwareBench.Core.Models;
using HardwareBench.Core.Detection.Monitor;

namespace HardwareBench.Tests.Detection;

public class MonitorDetectorTests
{
    private static byte[] ValidEdid()
    {
        var b = new byte[128];
        b[1] = b[2] = b[3] = b[4] = b[5] = b[6] = 0xFF;
        b[8] = 0x21; b[9] = 0x4C;          // "DEL"
        b[10] = 0xFF; b[11] = 0x40;
        b[17] = 30;                        // 2020 年
        b[72] = 0; b[73] = 0; b[74] = 0; b[75] = 0xFC;
        "U2720Q"u8.CopyTo(b.AsSpan(77));
        int sum = 0;
        for (int i = 0; i < 127; i++) sum += b[i];
        b[127] = (byte)(256 - sum % 256);
        return b;
    }

    private sealed class FakeSource : IEdidSource
    {
        public List<(string, byte[])> Items { get; } = [];
        public IEnumerable<(string InstancePath, byte[] Edid)> ReadAll() => Items;
    }

    [Fact]
    public async Task Detect_FillsMonitors_AndSkipsGarbageEntries()
    {
        var src = new FakeSource
        {
            Items =
            {
                (@"DISPLAY\DEL40FF\5&abc&0&0004", ValidEdid()),
                (@"DISPLAY\GARBAGE\1", new byte[128]) // 头不对 → 跳过，不抛
            }
        };
        var detector = new MonitorDetector(src);
        var report = new HardwareReport();

        await detector.DetectAsync(report, CancellationToken.None);

        var m = Assert.Single(report.Monitors);
        Assert.Equal("DEL", m.ManufacturerId);
        Assert.Equal("U2720Q", m.ModelName);
        Assert.Equal(2020, (int?)m.MadeYear);
        Assert.Equal(@"DISPLAY\DEL40FF\5&abc&0&0004", m.InstancePath);
    }
}