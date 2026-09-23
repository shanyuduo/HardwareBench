using System.Reflection;
using System.Security.Cryptography;

namespace HardwareBench.Core.Benchmarks;

public interface IToolLocator
{
    string GetToolPath(string toolName);
}

public interface IToolBinariesSource
{
    Stream? Open(string toolName);
}

public class ToolExtractor : IToolLocator
{
    private readonly IToolBinariesSource _binaries;
    private readonly string _rootDir;
    private readonly IReadOnlyDictionary<string, string> _expectedHashes;
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ToolExtractor(IToolBinariesSource binaries, string? rootDir = null, IReadOnlyDictionary<string, string>? hashOverrides = null)
    {
        _binaries = binaries;
        _rootDir = rootDir ?? Path.Combine(Path.GetTempPath(), "HardwareBench");
        _expectedHashes = hashOverrides ?? ToolHashes.Expected;
    }

    public string GetToolPath(string toolName)
    {
        if (_cache.TryGetValue(toolName, out var cached))
            return cached;

        var dir = Path.Combine(_rootDir, "tools");
        Directory.CreateDirectory(dir);

        var finalPath = Path.Combine(dir, toolName);

        if (File.Exists(finalPath) && VerifyHash(finalPath, toolName))
        {
            _cache[toolName] = finalPath;
            return finalPath;
        }

        var stream = _binaries.Open(toolName)
            ?? throw new InvalidOperationException($"Embedded tool resource not found: {toolName}");

        byte[] data;
        using (stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            data = ms.ToArray();
        }

        var actual = ComputeHash(data);
        if (!_expectedHashes.TryGetValue(toolName, out var expected))
            throw new InvalidOperationException($"Unknown tool hash: {toolName}");

        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Tool {toolName} failed integrity check: expected {expected[..8]}…, got {actual[..8]}…");

        var tmpPath = finalPath + ".tmp";
        try
        {
            using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(data, 0, data.Length);
                fs.Flush();
            }
            File.Move(tmpPath, finalPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);
            throw;
        }

        _cache[toolName] = finalPath;
        return finalPath;
    }

    private bool VerifyHash(string filePath, string toolName)
    {
        if (!_expectedHashes.TryGetValue(toolName, out var expected))
            return false;
        var actual = ComputeHash(File.ReadAllBytes(filePath));
        return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeHash(byte[] data)
        => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
}

public class AssemblyResourceBinariesSource : IToolBinariesSource
{
    public Stream? Open(string toolName)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream("HardwareBench.Tools." + toolName);
}