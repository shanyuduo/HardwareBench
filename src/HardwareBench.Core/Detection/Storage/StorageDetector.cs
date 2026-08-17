using System.Runtime.Versioning;
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection.Storage;

[SupportedOSPlatform("windows")]
public sealed class StorageDetector(NativeStorageApi api) : IHardwareDetector
{
    private static readonly Dictionary<uint, string> BusNames = new()
    {
        [0] = "Unknown", [1] = "Scsi", [2] = "Atapi", [3] = "Ata", [7] = "Usb",
        [8] = "1394", [11] = "Sas", [13] = "Sd", [15] = "Virtual", [17] = "Nvme"
    };

    public string Id => "storage.ioctl";

    public Task DetectAsync(HardwareReport report, CancellationToken ct)
    {
        foreach (var (path, descriptor, seekPenalty, tempC) in api.EnumeratePhysicalDrives())
        {
            ct.ThrowIfCancellationRequested();
            var d = StorageDescriptorParser.Parse(descriptor, seekPenalty, tempC);
            string model = string.Join(" ", new[] { d.Vendor, d.Product }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (model.Length == 0) model = path;
            report.Storage.Add(new StorageDevice(path, model, d.Serial, d.Revision,
                BusNames.GetValueOrDefault(d.BusType, $"Bus{d.BusType}"),
                d.IncursSeekPenalty ? false : true,
                d.TemperatureC, null));
        }
        return Task.CompletedTask;
    }
}