using System.Security.Cryptography;
using System.Text;
using HardwareBench.Core.Benchmarks;

namespace HardwareBench.Tests.Benchmarks;

public class ToolExtractorTests
{
    private static byte[] FakePayload(string tag) => Encoding.ASCII.GetBytes(tag);

    private static string Sha256Hex(byte[] data)
        => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    private sealed class FakeBinariesSource(Dictionary<string, byte[]> payloads) : IToolBinariesSource
    {
        public Stream? Open(string toolName)
            => payloads.TryGetValue(toolName, out var data)
                ? new MemoryStream(data)
                : null;
    }

    [Fact]
    public void Extract_ValidPayload_WritesFileAndVerifiesHash_ReuseDoesNotRewrite()
    {
        var payload = FakePayload("fake-diskspd-payload-v1");
        var hash = Sha256Hex(payload);
        var overrides = new Dictionary<string, string> { ["diskspd.exe"] = hash };
        var source = new FakeBinariesSource(new() { ["diskspd.exe"] = payload });

        var root = Path.Combine(Path.GetTempPath(), "HardwareBench-tests", Guid.NewGuid().ToString());
        var extractor = new ToolExtractor(source, root, overrides);

        var path = extractor.GetToolPath("diskspd.exe");
        Assert.NotNull(path);
        Assert.True(File.Exists(path));

        var written = File.ReadAllBytes(path);
        Assert.Equal(payload, written);

        var firstWrite = File.GetLastWriteTimeUtc(path);

        var path2 = extractor.GetToolPath("diskspd.exe");
        Assert.Equal(path, path2);
        Assert.Equal(firstWrite, File.GetLastWriteTimeUtc(path));

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Extract_TamperedStream_ThrowsInvalidOperationException()
    {
        var clean = FakePayload("fake-7zr-payload-v1");
        var tampered = (byte[])clean.Clone();
        tampered[0] ^= 0xFF;
        var hash = Sha256Hex(clean);
        var overrides = new Dictionary<string, string> { ["7zr.exe"] = hash };
        var source = new FakeBinariesSource(new() { ["7zr.exe"] = tampered });

        var root = Path.Combine(Path.GetTempPath(), "HardwareBench-tests", Guid.NewGuid().ToString());
        var extractor = new ToolExtractor(source, root, overrides);

        var ex = Assert.Throws<InvalidOperationException>(() => extractor.GetToolPath("7zr.exe"));
        Assert.Contains("7zr.exe", ex.Message);
        Assert.Contains(hash[..8], ex.Message);
        Assert.Contains(Sha256Hex(tampered)[..8], ex.Message);

        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void Extract_MissingResource_ThrowsInvalidOperationException()
    {
        var source = new FakeBinariesSource(new());
        var root = Path.Combine(Path.GetTempPath(), "HardwareBench-tests", Guid.NewGuid().ToString());
        var extractor = new ToolExtractor(source, root);

        var ex = Assert.Throws<InvalidOperationException>(() => extractor.GetToolPath("nonexistent.exe"));
        Assert.Contains("nonexistent.exe", ex.Message);
    }

    [Fact]
    public void AssemblyResourceBinariesSource_Probe_ContainsAllFiveToolNames()
    {
        var asm = typeof(ToolExtractor).Assembly;
        var names = asm.GetManifestResourceNames();
        Assert.Contains("HardwareBench.Tools.diskspd.exe", names);
        Assert.Contains("HardwareBench.Tools.7zr.exe", names);
        Assert.Contains("HardwareBench.Tools.stream-windows.exe", names);
        Assert.Contains("HardwareBench.Tools.7ZIP-LICENSE.txt", names);
        Assert.Contains("HardwareBench.Tools.DISKSPD-LICENSE.txt", names);
    }
}