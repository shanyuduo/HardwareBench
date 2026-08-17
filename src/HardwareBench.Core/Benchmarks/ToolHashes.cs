using System.Collections.ObjectModel;

namespace HardwareBench.Core.Benchmarks;

internal static class ToolHashes
{
    internal static readonly IReadOnlyDictionary<string, string> Expected = new ReadOnlyDictionary<string, string>(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["diskspd.exe"] = "8f3b2f0909549c54253edee26c9a8d239b8b6c817b076bcd7efb1bda6571aee9",
            ["7zr.exe"] = "56b8cc9f4971cef253644fafe54063ed7fdca551d4dee0f8c6baa81b855acd72",
            ["stream-windows.exe"] = "a09913f5ca7fc57aa755fc778b86cdfc6021abd8008e0dd08ac3fd05995a846e",
            ["7ZIP-LICENSE.txt"] = "3a184aa13dc8ad30734e28ff901b478ccbcb5b41be52427a5e7609c8fd9e5ddb",
            ["DISKSPD-LICENSE.txt"] = "40dc99f18435c2ee593b8e32aa084548d2894f71e8dcc9a4f03d83d54d78bf11",
        });

    internal const string ToolVersions = "DiskSpd v2.2; 7-Zip 7zr (bundled); STREAM 5.10 Windows-variant";
}