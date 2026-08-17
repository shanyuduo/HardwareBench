using System.Runtime.Versioning;
using Microsoft.Win32;

namespace HardwareBench.Core.Detection.Peripheral;

[SupportedOSPlatform("windows")]
public sealed class RegistryPeripheralSource
{
    private static readonly string[] Branches = ["USB", "HID", "USBSTOR"];

    public IEnumerable<RawEnumEntry> ReadAll()
    {
        foreach (var branch in Branches)
        {
            using var branchKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Enum\{branch}");
            if (branchKey is null) continue;
            foreach (var deviceId in branchKey.GetSubKeyNames())
            {
                using var deviceKey = branchKey.OpenSubKey(deviceId);
                if (deviceKey is null) continue;
                foreach (var instance in deviceKey.GetSubKeyNames())
                {
                    using var instanceKey = deviceKey.OpenSubKey(instance);
                    if (instanceKey is null) continue;
                    yield return new RawEnumEntry(
                        branch, $@"{branch}\{deviceId}\{instance}",
                        instanceKey.GetValue("DeviceDesc") as string,
                        instanceKey.GetValue("ClassGuid") as string);
                }
            }
        }
    }
}