using HardwareBench.Core.Detection.Peripheral;
using HardwareBench.Core.Models;

namespace HardwareBench.Tests.Detection;

public class PeripheralParserTests
{
    [Fact]
    public void Parse_StripsIndirectPrefix_AndMapsKinds()
    {
        var entries = new[]
        {
            new RawEnumEntry("USB", @"USB\VID_046D&PID_C08B\5&2d3f&0&2",
                "@oem39.inf,%devicedesc%;Logitech G502 HERO Gaming Mouse", "{4d36e96f-e325-11ce-bfc1-08002be10318}"),
            new RawEnumEntry("HID", @"HID\VID_046D&PID_C52B&MI_01\7&1a2b3c&0&0000",
                "USB 输入设备", "{745a17a0-74d3-11d0-b6fe-00a0c90f57da}"),
            new RawEnumEntry("USB", @"USB\VID_1A86&PID_7523\6&2f9d&0&1",
                null, "{36fc9e60-c465-11cf-8056-444553540000}"),
            new RawEnumEntry("USBSTOR", @"USBSTOR\Disk&Ven_Generic&Prod_SD_USB\...",
                "Generic SD USB Device", "{4d36e967-e325-11ce-bfc1-08002be10318}"),
            new RawEnumEntry("USB", @"USB\VID_046D&PID_C08B\5&2d3f&0&2",
                "@oem39.inf,%devicedesc%;Logitech G502 HERO Gaming Mouse", "{4d36e96f-e325-11ce-bfc1-08002be10318}")
        };

        var list = PeripheralParser.Parse(entries);

        Assert.Equal(2, list.Count);
        Assert.Equal("Mouse", list[0].Kind);
        Assert.Equal("Logitech G502 HERO Gaming Mouse", list[0].Name);
        Assert.Equal("HID 设备", list[1].Kind);
    }
}