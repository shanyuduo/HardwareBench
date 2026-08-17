using System.Runtime.Versioning;
using Microsoft.Win32;

namespace HardwareBench.Core.Detection.Monitor;

/// <summary>从注册表 HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY\<实例>\Device Parameters\EDID 读取。</summary>
[SupportedOSPlatform("windows")]
public sealed class RegistryEdidSource : IEdidSource
{
    public IEnumerable<(string InstancePath, byte[] Edid)> ReadAll()
    {
        using var display = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY");
        if (display is null) yield break;
        foreach (var device in display.GetSubKeyNames())
        {
            using var deviceKey = display.OpenSubKey(device);
            if (deviceKey is null) continue;
            foreach (var instance in deviceKey.GetSubKeyNames())
            {
                using var paramsKey = deviceKey.OpenSubKey($@"{instance}\Device Parameters");
                if (paramsKey?.GetValue("EDID") is not byte[] edid || edid.Length < 128) continue;
                yield return ($@"DISPLAY\{device}\{instance}", edid);
            }
        }
    }
}