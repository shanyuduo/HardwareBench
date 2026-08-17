# M1 硬件检测 · 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 M1——WPF 硬件检测页：CPU/主板/内存/显卡/存储/系统清单(WMI) + 屏幕(EDID) + 磁盘描述符信息(IOCTL) + 外设(注册表)，聚合为可导出 JSON 的 HardwareReport。

**Architecture:** 三工程单仓（App=WPF 壳 / Core=模型与检测服务 / Tests=xUnit）。检测器实现统一 `IHardwareDetector` 接口写入共享 `HardwareReport`，`DetectionService` 顺序执行并按检测器粒度容错。WMI 走 Hardware.Info（NuGet, MIT）；EDID/IOCTL/注册表为自写薄原生层 + 可单测的纯解析函数。

**Tech Stack:** .NET 8 (net8.0-windows for App, net8.0 for Core/Tests), C# 12, WPF + CommunityToolkit.Mvvm + Microsoft.Extensions.Hosting, Hardware.Info, System.Text.Json, xUnit。

## Global Constraints

- 仓库根 `REPO_ROOT` = `D:\OpenCodePortable\WORKSPACE\2026-08-17-硬件测评程序\`（下文相对路径均基于此）
- 目标框架：App=`net8.0-windows`；Core/Tests=`net8.0`；平台 win-x64
- `<Nullable>enable</Nullable>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- 本里程碑不引入 ring0 驱动、不读温度传感器（LHM 是 M3 内容）
- 范围微调（相对 design.md §4.1，已批准原则下的实现细节）：SMART 深度属性表（ATA PASS_THROUGH）移至 M3；M1 存储项交付描述符级信息（型号/序列号/固件/总线/SSD 判定/温度，温度查询不支持时置 null）
- UI 文案中文，集中在 `Strings.zh-CN.xaml` 资源字典
- TDD：每个行为先写失败测试；提交信息用 conventional commits（feat/test/chore/docs）
- 每个任务收尾必须 `dotnet build` 零警告零错误后才 commit
- 本项目许可证 MIT；发布物携带 THIRDPARTY-NOTICES.md

## 文件结构总览

```
REPO_ROOT/
├─ .gitignore
├─ README.md                          # Task 10
├─ THIRDPARTY-NOTICES.md              # Task 10
├─ publish.ps1                        # Task 10
├─ Directory.Build.props              # Task 1（全仓公共属性）
├─ docs/plans/2026-08-17-m1-detection.md   # 本计划
└─ src/
   ├─ HardwareBench.sln
   ├─ HardwareBench.Core/
   │  ├─ Models/HardwareReport.cs     # Task 2 聚合根 + OsInfo
   │  ├─ Models/ComputerModels.cs     # Task 2 CpuInfo/MotherboardInfo/MemoryModule/GpuInfo
   │  ├─ Models/DeviceModels.cs       # Task 2 StorageDevice/MonitorInfo/PeripheralInfo
   │  ├─ Models/DetectionError.cs     # Task 2
   │  ├─ Detection/DetectionService.cs        # Task 8
   │  ├─ Detection/IHardwareDetector.cs       # Task 8
   │  ├─ Detection/Wmi/IWmiSource.cs          # Task 7
   │  ├─ Detection/Wmi/HardwareInfoWmiSource.cs # Task 7（薄，不单测）
   │  ├─ Detection/Wmi/WmiInventoryDetector.cs  # Task 7 映射逻辑（单测）
   │  ├─ Detection/Edid/EdidParser.cs          # Task 3 纯函数
   │  ├─ Detection/Edid/EdidData.cs            # Task 3
   │  ├─ Detection/Monitor/MonitorDetector.cs  # Task 4 聚合（单测）
   │  ├─ Detection/Monitor/IEdidSource.cs      # Task 4
   │  ├─ Detection/Monitor/RegistryEdidSource.cs # Task 4 薄注册表层
   │  ├─ Detection/Storage/StorageDescriptorParser.cs # Task 5 纯函数（单测）
   │  ├─ Detection/Storage/NativeStorageApi.cs # Task 5 P/Invoke 薄层
   │  ├─ Detection/Storage/StorageDetector.cs  # Task 5 编排（薄）
   │  ├─ Detection/Peripheral/PeripheralParser.cs # Task 6 纯函数（单测）
   │  ├─ Detection/Peripheral/RegistryPeripheralSource.cs # Task 6 薄层
   │  └─ Serialization/ReportJson.cs   # Task 2 JSON 序列化（单测）
   └─ HardwareBench.App/
      ├─ App.xaml / App.xaml.cs        # Task 9 Generic Host + DI
      ├─ Strings.zh-CN.xaml            # Task 9
      ├─ MainWindow.xaml(.cs)          # Task 9 侧边栏导航
      ├─ Views/DetectionPage.xaml(.cs) # Task 9
      ├─ Views/PlaceholderPage.xaml(.cs) # Task 9（跑分/历史占位文案）
      └─ ViewModels/DetectionViewModel.cs # Task 9（单测）
```

---

### Task 1: 仓库与解决方案脚手架

**Files:**
- Create: `.gitignore`, `Directory.Build.props`, `src/HardwareBench.sln`, `src/HardwareBench.Core/HardwareBench.Core.csproj`, `src/HardwareBench.Tests/HardwareBench.Tests.csproj`, `src/HardwareBench.App/HardwareBench.App.csproj`, `src/HardwareBench.Tests/SmokeTests.cs`

**Interfaces:**
- Produces: 可构建的三工程解决方案；Tests 引用 Core；App 引用 Core。后续所有任务的工程名/目标框架以此为准。

- [ ] **Step 1: 初始化仓库与目录**

```powershell
# workdir: REPO_ROOT
git init
New-Item -ItemType Directory -Force -Path src | Out-Null
```

- [ ] **Step 2: 写 .gitignore 与 Directory.Build.props**

`.gitignore`（标准 VS 模板，确保含以下关键行）：

```gitignore
bin/
obj/
publish/
*.user
.vs/
TestResults/
data/
```

`Directory.Build.props`：

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: 创建三个工程**

```powershell
# workdir: REPO_ROOT\src
dotnet new classlib -n HardwareBench.Core -f net8.0
dotnet new xunit -n HardwareBench.Tests -f net8.0
dotnet new wpf -n HardwareBench.App -f net8.0
dotnet sln add HardwareBench.Core HardwareBench.Tests HardwareBench.App
dotnet add HardwareBench.Tests reference HardwareBench.Core
dotnet add HardwareBench.App reference HardwareBench.Core
```

`HardwareBench.App.csproj` 的 `<PropertyGroup>` 中追加（若模板未生成）：

```xml
<AssemblyName>HardwareBench</AssemblyName>
<ApplicationManifest>app.manifest</ApplicationManifest>
```

（`dotnet new wpf` 生成的 app.manifest 保持默认 asInvoker——M1 不需要管理员。）

- [ ] **Step 4: 写冒烟测试（保证测试基建可用）**

替换 `src/HardwareBench.Tests/SmokeTests.cs`（删除模板生成的 UnitTest1.cs）：

```csharp
namespace HardwareBench.Tests;

public class SmokeTests
{
    [Fact]
    public void TestFramework_Runs()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 5: 构建并跑测试，验证零警告**

```powershell
# workdir: REPO_ROOT\src
dotnet build HardwareBench.sln -c Debug
dotnet test HardwareBench.Tests -c Debug --no-build
```

Expected: Build succeeded 0 Warning(s) 0 Error(s)；测试 1 passed。

- [ ] **Step 6: Commit**

```powershell
# workdir: REPO_ROOT
git add .gitignore Directory.Build.props src
git commit -m "chore: scaffold solution (Core/Tests/App)"
```

---

### Task 2: HardwareReport 领域模型 + JSON 序列化

**Files:**
- Create: `src/HardwareBench.Core/Models/HardwareReport.cs`, `src/HardwareBench.Core/Models/ComputerModels.cs`, `src/HardwareBench.Core/Models/DeviceModels.cs`, `src/HardwareBench.Core/Models/DetectionError.cs`, `src/HardwareBench.Core/Serialization/ReportJson.cs`
- Test: `src/HardwareBench.Tests/Models/HardwareReportJsonTests.cs`

**Interfaces:**
- Produces（后续所有任务依赖的准确类型）：
  - `HardwareReport { DateTimeOffset CapturedAtUtc; OsInfo? Os; CpuInfo? Cpu; MotherboardInfo? Motherboard; List<MemoryModule> MemoryModules; List<GpuInfo> Gpus; List<StorageDevice> Storage; List<MonitorInfo> Monitors; List<PeripheralInfo> Peripherals; List<DetectionError> Errors; }`
  - `OsInfo { string Name; string Version; string BuildNumber; string Architecture; }`
  - `CpuInfo { string Name; string? ProcessorId; int PhysicalCores; int LogicalProcessors; int MaxClockSpeedMhz; long? L3CacheKb; }`
  - `MotherboardInfo { string Manufacturer; string Product; string? SerialNumber; }`
  - `MemoryModule { string BankLabel; string DeviceLocator; ulong CapacityBytes; int SpeedMts; string? Manufacturer; string? PartNumber; string? SerialNumber; string? FormFactor; }`
  - `GpuInfo { string Name; string? Vendor; ulong? DedicatedMemoryBytes; string? DriverVersion; string? VideoProcessor; }`
  - `StorageDevice { string DevicePath; string Model; string? SerialNumber; string? FirmwareRevision; string BusType; bool? IsSsd; int? TemperatureC; ulong? SizeBytes; }`
  - `MonitorInfo { string InstancePath; string ManufacturerId; ushort ProductCode; string? ModelName; string? SerialString; ushort? MadeYear; byte? MadeWeek; }`
  - `PeripheralInfo { string Kind; string Name; string InstancePath; }`
  - `DetectionError { string DetectorId; string Message; }`
  - `static class ReportJson { static string Serialize(HardwareReport report); static HardwareReport Deserialize(string json); }`

- [ ] **Step 1: 写失败测试**

`src/HardwareBench.Tests/Models/HardwareReportJsonTests.cs`：

```csharp
using HardwareBench.Core.Models;
using HardwareBench.Core.Serialization;

namespace HardwareBench.Tests.Models;

public class HardwareReportJsonTests
{
    [Fact]
    public void RoundTrip_PreservesAllSections()
    {
        var report = new HardwareReport
        {
            CapturedAtUtc = new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero),
            Os = new OsInfo("Microsoft Windows 11 Pro", "10.0.26100", "26100", "64-bit"),
            Cpu = new CpuInfo("Intel Core i7-13700K", "BFEBFBFF000B067A", 16, 24, 5400, 36864),
            Motherboard = new MotherboardInfo("ASUSTeK", "ROG STRIX Z790-E", "MB-123456789"),
            MemoryModules =
            {
                new MemoryModule("BANK 0", "ChannelA-DIMM1", 34359738368, 5600,
                    " Kingston", "KF556C36BBE-16", "12345678", "SODIMM")
            },
            Gpus = { new GpuInfo("NVIDIA GeForce RTX 4070", "NVIDIA", 12247367680, "32.0.15.6094", null) },
            Storage =
            {
                new StorageDevice(@"\\.\PhysicalDrive0", "Samsung SSD 990 PRO 2TB",
                    "S6Z1NJ0R123456", "1B2QJXD7", "Nvme", true, 41, 2048408248320)
            },
            Monitors =
            {
                new MonitorInfo(@"DISPLAY\DEL40FF\5&2b3c4d5&0&0004", "DEL", 0x40FF,
                    "U2415F", "CN0123456789", 2016, 12)
            },
            Peripherals = { new PeripheralInfo("Mouse", "Logitech G Pro Wireless", @"USB\VID_046D&PID_C088\12345678") },
            Errors = { new DetectionError("test.detector", "boom") }
        };

        var json = ReportJson.Serialize(report);
        var back = ReportJson.Deserialize(json);

        Assert.Equal(report.Cpu!.Name, back.Cpu!.Name);
        Assert.Equal(report.Cpu.LogicalProcessors, back.Cpu.LogicalProcessors);
        Assert.Equal(report.MemoryModules[0].CapacityBytes, back.MemoryModules[0].CapacityBytes);
        Assert.Equal(report.Storage[0].TemperatureC, back.Storage[0].TemperatureC);
        Assert.Equal(report.Monitors[0].ProductCode, back.Monitors[0].ProductCode);
        Assert.Equal(report.Peripherals[0].Name, back.Peripherals[0].Name);
        Assert.Equal(report.Errors[0].DetectorId, back.Errors[0].DetectorId);
        Assert.Equal(report.CapturedAtUtc, back.CapturedAtUtc);
    }
}
```

- [ ] **Step 2: 运行确认失败**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~HardwareReportJsonTests
```

Expected: 编译失败（Models/Serialization 不存在）。

- [ ] **Step 3: 实现模型与序列化**

`src/HardwareBench.Core/Models/HardwareReport.cs`：

```csharp
namespace HardwareBench.Core.Models;

public sealed class HardwareReport
{
    public DateTimeOffset CapturedAtUtc { get; set; }
    public OsInfo? Os { get; set; }
    public CpuInfo? Cpu { get; set; }
    public MotherboardInfo? Motherboard { get; set; }
    public List<MemoryModule> MemoryModules { get; set; } = [];
    public List<GpuInfo> Gpus { get; set; } = [];
    public List<StorageDevice> Storage { get; set; } = [];
    public List<MonitorInfo> Monitors { get; set; } = [];
    public List<PeripheralInfo> Peripherals { get; set; } = [];
    public List<DetectionError> Errors { get; set; } = [];
}

public sealed record OsInfo(string Name, string Version, string BuildNumber, string Architecture);
```

`src/HardwareBench.Core/Models/ComputerModels.cs`：

```csharp
namespace HardwareBench.Core.Models;

public sealed record CpuInfo(
    string Name, string? ProcessorId, int PhysicalCores, int LogicalProcessors,
    int MaxClockSpeedMhz, long? L3CacheKb);

public sealed record MotherboardInfo(string Manufacturer, string Product, string? SerialNumber);

public sealed record MemoryModule(
    string BankLabel, string DeviceLocator, ulong CapacityBytes, int SpeedMts,
    string? Manufacturer, string? PartNumber, string? SerialNumber, string? FormFactor);

public sealed record GpuInfo(
    string Name, string? Vendor, ulong? DedicatedMemoryBytes,
    string? DriverVersion, string? VideoProcessor);
```

`src/HardwareBench.Core/Models/DeviceModels.cs`：

```csharp
namespace HardwareBench.Core.Models;

public sealed record StorageDevice(
    string DevicePath, string Model, string? SerialNumber, string? FirmwareRevision,
    string BusType, bool? IsSsd, int? TemperatureC, ulong? SizeBytes);

public sealed record MonitorInfo(
    string InstancePath, string ManufacturerId, ushort ProductCode,
    string? ModelName, string? SerialString, ushort? MadeYear, byte? MadeWeek);

public sealed record PeripheralInfo(string Kind, string Name, string InstancePath);
```

`src/HardwareBench.Core/Models/DetectionError.cs`：

```csharp
namespace HardwareBench.Core.Models;

public sealed record DetectionError(string DetectorId, string Message);
```

`src/HardwareBench.Core/Serialization/ReportJson.cs`：

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Serialization;

public static class ReportJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(HardwareReport report)
        => JsonSerializer.Serialize(report, Options);

    public static HardwareReport Deserialize(string json)
        => JsonSerializer.Deserialize<HardwareReport>(json)
           ?? throw new JsonException("反序列化结果为 null。");
}
```

- [ ] **Step 4: 运行确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~HardwareReportJsonTests
```

Expected: 1 passed。

- [ ] **Step 5: Commit**

```powershell
# workdir: REPO_ROOT
git add src
git commit -m "feat: HardwareReport domain models with JSON roundtrip"
```

---

### Task 3: EDID 解析器（纯函数）

**Files:**
- Create: `src/HardwareBench.Core/Detection/Edid/EdidData.cs`, `src/HardwareBench.Core/Detection/Edid/EdidParser.cs`
- Test: `src/HardwareBench.Tests/Detection/EdidParserTests.cs`

**Interfaces:**
- Produces:
  - `EdidData { string ManufacturerId; ushort ProductCode; uint SerialNumber; byte MadeWeek; ushort MadeYear; byte VersionMajor; byte VersionMinor; string? ModelName; string? SerialString; byte ExtensionCount; bool ChecksumValid; }`
  - `static class EdidParser { static EdidData Parse(ReadOnlySpan<byte> edid); }` — 头 8 字节非 `00 FF FF FF FF FF FF 00` 时抛 `FormatException("Invalid EDID header")`；长度 <128 抛 `ArgumentException`

- [ ] **Step 1: 写失败测试（含 checksum 修正辅助）**

`src/HardwareBench.Tests/Detection/EdidParserTests.cs`：

```csharp
using HardwareBench.Core.Detection.Edid;

namespace HardwareBench.Tests.Detection;

public class EdidParserTests
{
    /// <summary>构造 128 字节 EDID 基块，并修正第 127 字节使整块校验和为 0 (mod 256)。</summary>
    private static byte[] BuildEdid(Action<byte[]> mutate)
    {
        var b = new byte[128];
        b[0] = 0x00; b[1] = 0xFF; b[2] = 0xFF; b[3] = 0xFF;
        b[4] = 0xFF; b[5] = 0xFF; b[6] = 0xFF; b[7] = 0x00;
        b[8] = 0x21; b[9] = 0x4C;            // 厂商 ID "DEL"（D=4,E=5,L=12）
        b[10] = 0xFF; b[11] = 0x40;          // 产品码 0x40FF (LE)
        b[12] = 0x78; b[13] = 0x56; b[14] = 0x34; b[15] = 0x12; // 序列号 0x12345678 (LE)
        b[16] = 23;                          // 生产周
        b[17] = 36;                          // 生产年 = 1990+36 = 2026
        b[18] = 1; b[19] = 4;                // EDID 1.4
        // 描述符块 2（偏移 72）：监视器名称 "U2415F"
        b[72] = 0; b[73] = 0; b[74] = 0; b[75] = 0xFC;
        var name = "U2415F"u8;
        name.CopyTo(b.AsSpan(77));
        // 描述符块 3（偏移 90）：序列号字符串 "CN0123456789"
        b[90] = 0; b[91] = 0; b[92] = 0; b[93] = 0xFF;
        var sn = "CN0123456789"u8;
        sn.CopyTo(b.AsSpan(95));
        mutate(b);
        int sum = 0;
        for (int i = 0; i < 127; i++) sum += b[i];
        b[127] = (byte)((256 - (sum % 256)) % 256);
        return b;
    }

    [Fact]
    public void Parse_ReadsAllPlantedFields()
    {
        var edid = BuildEdid(_ => { });

        var d = EdidParser.Parse(edid);

        Assert.Equal("DEL", d.ManufacturerId);
        Assert.Equal(0x40FF, d.ProductCode);
        Assert.Equal(0x12345678u, d.SerialNumber);
        Assert.Equal(23, d.MadeWeek);
        Assert.Equal(2026, d.MadeYear);
        Assert.Equal(1, d.VersionMajor);
        Assert.Equal(4, d.VersionMinor);
        Assert.Equal("U2415F", d.ModelName);
        Assert.Equal("CN0123456789", d.SerialString);
        Assert.True(d.ChecksumValid);
    }

    [Fact]
    public void Parse_CorruptChecksum_ReportsInvalidButParses()
    {
        var edid = BuildEdid(b => b[40] ^= 0xFF); // 破坏一个字节 → 校验和必失效

        var d = EdidParser.Parse(edid);

        Assert.False(d.ChecksumValid);
        Assert.Equal("DEL", d.ManufacturerId);
    }

    [Fact]
    public void Parse_BadHeader_Throws()
    {
        var edid = BuildEdid(b => b[1] = 0x00);

        Assert.Throws<FormatException>(() => EdidParser.Parse(edid));
    }

    [Fact]
    public void Parse_TooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => EdidParser.Parse(new byte[127]));
    }
}
```

- [ ] **Step 2: 运行确认失败**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~EdidParserTests
```

Expected: 编译失败（Edid 命名空间不存在）。

- [ ] **Step 3: 实现 EdidData 与 EdidParser**

`src/HardwareBench.Core/Detection/Edid/EdidData.cs`：

```csharp
namespace HardwareBench.Core.Detection.Edid;

public sealed record EdidData(
    string ManufacturerId, ushort ProductCode, uint SerialNumber,
    byte MadeWeek, ushort MadeYear, byte VersionMajor, byte VersionMinor,
    string? ModelName, string? SerialString, byte ExtensionCount, bool ChecksumValid);
```

`src/HardwareBench.Core/Detection/Edid/EdidParser.cs`：

```csharp
namespace HardwareBench.Core.Detection.Edid;

/// <summary>解析 128 字节 EDID 1.x 基块。纯函数，无副作用。</summary>
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
```

- [ ] **Step 4: 运行确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~EdidParserTests
```

Expected: 4 passed。

- [ ] **Step 5: Commit**

```powershell
# workdir: REPO_ROOT
git add src
git commit -m "feat: EDID 1.x base block parser"
```

---

### Task 4: 显示器检测器（注册表 EDID 源 + 聚合）

**Files:**
- Create: `src/HardwareBench.Core/Detection/Monitor/IEdidSource.cs`, `src/HardwareBench.Core/Detection/Monitor/MonitorDetector.cs`, `src/HardwareBench.Core/Detection/Monitor/RegistryEdidSource.cs`
- Test: `src/HardwareBench.Tests/Detection/MonitorDetectorTests.cs`

**Interfaces:**
- Consumes: `EdidParser.Parse`（Task 3）、`MonitorInfo`（Task 2）
- Produces:
  - `interface IEdidSource { IEnumerable<(string InstancePath, byte[] Edid)> ReadAll(); }`
  - `class MonitorDetector : IHardwareDetector`（接口在 Task 8 定义；本任务先以方法签名形式存在，Task 8 接入容器）：
    `string Id => "monitor.edid"; Task DetectAsync(HardwareReport report, CancellationToken ct);`

- [ ] **Step 1: 写失败测试**

`src/HardwareBench.Tests/Detection/MonitorDetectorTests.cs`：

```csharp
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
        Assert.Equal(2020, m.MadeYear);
        Assert.Equal(@"DISPLAY\DEL40FF\5&abc&0&0004", m.InstancePath);
    }
}
```

- [ ] **Step 2: 运行确认失败**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~MonitorDetectorTests
```

Expected: 编译失败。

- [ ] **Step 3: 实现三个文件**

`src/HardwareBench.Core/Detection/Monitor/IEdidSource.cs`：

```csharp
namespace HardwareBench.Core.Detection.Monitor;

public interface IEdidSource
{
    IEnumerable<(string InstancePath, byte[] Edid)> ReadAll();
}
```

`src/HardwareBench.Core/Detection/Monitor/MonitorDetector.cs`：

```csharp
using HardwareBench.Core.Detection.Edid;
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection.Monitor;

public sealed class MonitorDetector(IEdidSource source)
{
    public string Id => "monitor.edid";

    public Task DetectAsync(HardwareReport report, CancellationToken ct)
    {
        foreach (var (instancePath, edid) in source.ReadAll())
        {
            ct.ThrowIfCancellationRequested();
            EdidData d;
            try { d = EdidParser.Parse(edid); }
            catch (FormatException) { continue; } // 无效块：跳过该显示器
            report.Monitors.Add(new MonitorInfo(
                instancePath, d.ManufacturerId, d.ProductCode,
                d.ModelName, d.SerialString, d.MadeYear, d.MadeWeek));
        }
        return Task.CompletedTask;
    }
}
```

`src/HardwareBench.Core/Detection/Monitor/RegistryEdidSource.cs`（薄注册表层，不单测）：

```csharp
using Microsoft.Win32;

namespace HardwareBench.Core.Detection.Monitor;

/// <summary>从注册表 HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY\<实例>\Device Parameters\EDID 读取。</summary>
public sealed class RegistryEdidSource : IEdidSource
{
    public IEnumerable<(string InstancePath, byte[] Edid)> ReadAll()
    {
        using var display = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY")
            ?? yield break;
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
```

- [ ] **Step 4: 运行确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~MonitorDetectorTests
```

Expected: 1 passed。

- [ ] **Step 5: 真机冒烟（证据采集）**

```powershell
# workdir: REPO_ROOT\src
dotnet run --project HardwareBench.Tests -- --list-tests | Out-Null  # 仅确认可运行集
```

真机验证并入 Task 9 的手动 QA（此处注册表层逻辑极薄）。

- [ ] **Step 6: Commit**

```powershell
# workdir: REPO_ROOT
git add src
git commit -m "feat: monitor detector over registry EDID"
```

---

### Task 5: 存储检测器（IOCTL 描述符查询）

**Files:**
- Create: `src/HardwareBench.Core/Detection/Storage/StorageDescriptorParser.cs`, `src/HardwareBench.Core/Detection/Storage/NativeStorageApi.cs`, `src/HardwareBench.Core/Detection/Storage/StorageDetector.cs`
- Test: `src/HardwareBench.Tests/Detection/StorageDescriptorParserTests.cs`

**Interfaces:**
- Consumes: `StorageDevice`（Task 2）
- Produces:
  - `static class StorageDescriptorParser { static ParsedDescriptor Parse(byte[] buffer); }`，
    `record ParsedDescriptor(string? Vendor, string? Product, string? Revision, string? Serial, uint BusType, bool IncursSeekPenalty, int? TemperatureC)`
  - `class StorageDetector : INativeDriveEnumerator` 编排：`string Id => "storage.ioctl"; Task DetectAsync(HardwareReport, CancellationToken)`

**实现要点（P/Invoke 事实核对清单）：**
- `STORAGE_DEVICE_DESCRIPTOR` 布局（winioctl.h）：`Version(0,4) Size(4,4) DeviceType(8,1) DeviceTypeModifier(9,1) RemovableMedia(10,1) CommandQueueing(11,1) VendorIdOffset(12,4) ProductIdOffset(16,4) ProductRevisionOffset(20,4) SerialNumberOffset(24,4) BusType(28,4) RawPropertiesLength(32,4) RawDeviceProperties(36..)`。字符串以 `\0` 结尾追加在缓冲区尾部，偏移为 0 表示不存在。**实现前用 winioctl.h / Microsoft Learn 交叉核对每个枚举值**：
  - `STORAGE_PROPERTY_QUERY` PropertyId：`StorageDeviceProperty = 0`、`StorageDeviceSeekPenalty = 7`、`StorageDeviceTemperatureProperty = 51`（如与头文件不符，以头文件为准并更新本文件常量）。
  - `DEVICE_SEEK_PENALTY_DESCRIPTOR { DWORD Version; DWORD Size; BOOLEAN IncursSeekPenalty; }`
  - `STORAGE_TEMPERATURE_DATA_DESCRIPTOR { DWORD Version; DWORD Size; SHORT CriticalCount...; WORD Count; STORAGE_TEMPERATURE_INFO Info[1]; }` —— 只取第一个 `Info` 的 `Temperature`（SHORT，摄氏度；`-32768` 表示未知 → null）。

- [ ] **Step 1: 写失败测试（描述符字节布局 fixture）**

`src/HardwareBench.Tests/Detection/StorageDescriptorParserTests.cs`：

```csharp
using HardwareBench.Core.Detection.Storage;

namespace HardwareBench.Tests.Detection;

public class StorageDescriptorParserTests
{
    private static byte[] BuildBuffer()
    {
        // 布局：36 字节固定头 + 字符串区
        var b = new byte[128];
        WriteU32(b, 0, 36 + 11 + 17 + 4 + 13);          // Size
        WriteU32(b, 12, 36);                             // VendorIdOffset → "Samsung "
        WriteU32(b, 16, 47);                             // ProductIdOffset → "SSD 990 PRO 2TB"
        WriteU32(b, 20, 63);                             // ProductRevisionOffset → "1B2Q"
        WriteU32(b, 24, 67);                             // SerialNumberOffset → "S6Z1NJ0R12345"
        WriteU32(b, 28, 17);                             // BusType = BusTypeNvme (17)
        System.Text.Encoding.ASCII.GetBytes("Samsung ").CopyTo(b, 36);
        System.Text.Encoding.ASCII.GetBytes("SSD 990 PRO 2TB").CopyTo(b, 47);
        System.Text.Encoding.ASCII.GetBytes("1B2Q").CopyTo(b, 63);
        System.Text.Encoding.ASCII.GetBytes("S6Z1NJ0R12345").CopyTo(b, 67);
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
        Assert.Null(d.TemperatureC); // 温度不在此缓冲区解析
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
        WriteU32(b, 16, 0); // Product 关闭
        // Vendor "Samsung "（带尾空格）应 TrimEnd
        var d = StorageDescriptorParser.Parse(b);
        Assert.Equal("Samsung", d.Vendor);
    }
}
```

- [ ] **Step 2: 运行确认失败**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~StorageDescriptorParserTests
```

Expected: 编译失败。

- [ ] **Step 3: 实现解析器**

`src/HardwareBench.Core/Detection/Storage/StorageDescriptorParser.cs`：

```csharp
using System.Buffers.Binary;

namespace HardwareBench.Core.Detection.Storage;

public sealed record ParsedDescriptor(
    string? Vendor, string? Product, string? Revision, string? Serial,
    uint BusType, bool IncursSeekPenalty, int? TemperatureC);

/// <summary>解析 STORAGE_DEVICE_DESCRIPTOR 原始缓冲区（纯函数）。
/// 温度与寻道惩罚不在此缓冲区，由 NativeStorageApi 单独查询后合并。</summary>
public static class StorageDescriptorParser
{
    public static ParsedDescriptor Parse(byte[] buffer, bool incursSeekPenalty = false, int? temperatureC = null)
    {
        if (buffer.Length < 36)
            throw new ArgumentException("缓冲区小于 STORAGE_DEVICE_DESCRIPTOR 固定头。", nameof(buffer));

        string? ReadString(uint offset) => offset == 0 || offset >= buffer.Length
            ? null
            : ReadNullTerminated(buffer, (int)offset);

        return new ParsedDescriptor(
            Vendor: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(12))),
            Product: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(16))),
            Revision: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(20))),
            Serial: ReadString(BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(24))),
            BusType: BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(28)),
            IncursSeekPenalty: incursSeekPenalty,
            TemperatureC: temperatureC);
    }

    private static string? ReadNullTerminated(byte[] buffer, int offset)
    {
        int end = offset;
        while (end < buffer.Length && buffer[end] != 0) end++;
        var s = System.Text.Encoding.ASCII.GetString(buffer, offset, end - offset).TrimEnd();
        return s.Length == 0 ? null : s;
    }
}
```

（注意：测试中 `Parse(BuildBuffer())` 依赖默认参数签名 `Parse(byte[], bool, int?)`——测试编译通过即可。）

- [ ] **Step 4: 运行确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~StorageDescriptorParserTests
```

Expected: 3 passed。

- [ ] **Step 5: 实现 P/Invoke 薄层与编排器**

`src/HardwareBench.Core/Detection/Storage/NativeStorageApi.cs`：

```csharp
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace HardwareBench.Core.Detection.Storage;

/// <summary>P/Invoke 薄层。枚举 \\.\PhysicalDrive0..15，描述符/寻道惩罚/温度三类查询。
/// 常量值实现前对照 winioctl.h 核对（见计划核对清单）。</summary>
public sealed class NativeStorageApi
{
    private const uint PropertyStandardDefine = 0;
    private const uint StorageDeviceProperty = 0;
    private const uint StorageDeviceSeekPenalty = 7;
    private const uint StorageDeviceTemperatureProperty = 51;
    private const uint IoctlStorageQueryProperty = 0x002D1400;

    public IEnumerable<(string Path, byte[] Descriptor, bool SeekPenalty, int? TempC)> EnumeratePhysicalDrives()
    {
        for (int i = 0; i < 16; i++)
        {
            string path = $@"\\.\PhysicalDrive{i}";
            SafeHandle? handle = TryOpen(path);
            if (handle is null || handle.IsInvalid) { handle?.Dispose(); continue; }
            using (handle)
            {
                byte[]? desc = QueryDescriptor(handle, StorageDeviceProperty, 4096);
                if (desc is null) continue; // 无介质/不可查询
                bool seek = QuerySeekPenalty(handle);
                int? temp = QueryTemperature(handle);
                yield return (path, desc, seek, temp);
            }
        }
    }

    private static SafeHandle? TryOpen(string path)
    {
        try
        {
            var h = NativeMethods.CreateFile(path, NativeMethods.GenericRead,
                FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
            return h.IsInvalid ? null : h;
        }
        catch (Win32Exception) { return null; }
    }

    private static byte[]? QueryDescriptor(SafeHandle handle, uint propertyId, int size)
    {
        var query = new byte[8 + sizeof(uint) * 3]; // STORAGE_PROPERTY_QUERY{PropertyId, AdditionalInfo, QueryType} + padding
        // PropertyId (4) | AdditionalInfo[0] 对齐 (4) | QueryType (4) → 按头文件布局写：
        // [0..3]=PropertyId, [4..7]=对齐保留, [8..11]=PropertyStandardDefine
        query[0] = (byte)propertyId; query[1] = (byte)(propertyId >> 8);
        query[2] = (byte)(propertyId >> 16); query[3] = (byte)(propertyId >> 24);
        query[8] = (byte)PropertyStandardDefine;
        return DeviceIoControlRead(handle, IoctlStorageQueryProperty, query, size);
    }

    private static bool QuerySeekPenalty(SafeHandle handle)
    {
        var outBuf = QueryDescriptor(handle, StorageDeviceSeekPenalty, 64);
        return outBuf is { Length: >= 12 } && outBuf[8] != 0; // DEVICE_SEEK_PENALTY_DESCRIPTOR.IncursSeekPenalty @8
    }

    private static int? QueryTemperature(SafeHandle handle)
    {
        var outBuf = QueryDescriptor(handle, StorageDeviceTemperatureProperty, 1024);
        if (outBuf is null || outBuf.Length < 16) return null;
        short raw = (short)(outBuf[14] | (outBuf[15] << 8)); // 首个 Info.Temperature（SHORT）
        return raw == short.MinValue ? null : raw;
    }

    private static byte[]? DeviceIoControlRead(SafeHandle handle, uint ioctl, byte[] input, int outSize)
    {
        var output = new byte[outSize];
        if (!NativeMethods.DeviceIoControl(handle, ioctl, input, (uint)input.Length,
                output, (uint)outSize, out uint returned, IntPtr.Zero))
            return null;
        Array.Resize(ref output, (int)returned);
        return output;
    }

    private static class NativeMethods
    {
        public const uint GenericRead = 0x80000000;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeHandle CreateFile(string lpFileName, uint dwDesiredAccess,
            FileShare dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(SafeHandle hDevice, uint dwIoControlCode,
            byte[] lpInBuffer, uint nInBufferSize, byte[] lpOutBuffer, uint nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);
    }
}
```

`src/HardwareBench.Core/Detection/Storage/StorageDetector.cs`：

```csharp
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection.Storage;

public sealed class StorageDetector(NativeStorageApi api)
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
                seekPenalty ? false : null, // 有寻道惩罚 → HDD(false)；无 → SSD(true)？保守：仅 HDD 判定为 false，否则 null
                d.TemperatureC, null));
        }
        return Task.CompletedTask;
    }
}
```

（`IsSsd` 语义：`IncursSeekPenalty==true` → HDD(false)；`==false` → SSD(true)——把上面保守写法改为 `seekPenalty ? false : true`，并在 UI 显示为 HDD/SSD；若查询本身失败无法判定才是 null。按此修正实现。）

- [ ] **Step 6: 构建零警告 + 全量测试**

```powershell
# workdir: REPO_ROOT\src
dotnet build HardwareBench.sln
dotnet test HardwareBench.Tests --no-build
```

Expected: 全部通过（含既有测试）。

- [ ] **Step 7: Commit**

```powershell
# workdir: REPO_ROOT
git add src
git commit -m "feat: storage detector via IOCTL descriptor queries"
```

---

### Task 6: 外设枚举（注册表源 + 解析纯函数）

**Files:**
- Create: `src/HardwareBench.Core/Detection/Peripheral/PeripheralParser.cs`, `src/HardwareBench.Core/Detection/Peripheral/RegistryPeripheralSource.cs`
- Test: `src/HardwareBench.Tests/Detection/PeripheralParserTests.cs`

**Interfaces:**
- Consumes: `PeripheralInfo`（Task 2）
- Produces:
  - `record RawEnumEntry(string Branch, string InstancePath, string? DeviceDesc, string? ClassGuid)`（DeviceDesc 可能是 `"@oem39.inf,%mfg%;罗技 G Pro"` 注册表间接格式）
  - `static class PeripheralParser { static List<PeripheralInfo> Parse(IEnumerable<RawEnumEntry> entries); }`——去 `@inf,%key%;` 前缀、按 Class 过滤（Mouse/Keyboard/Media/USBSTOR/HIDClass）、按 InstancePath 去重

- [ ] **Step 1: 写失败测试**

`src/HardwareBench.Tests/Detection/PeripheralParserTests.cs`：

```csharp
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
                "@oem39.inf,%devicedesc%;Logitech G502 HERO Gaming Mouse", "{4d36e96f-e325-11ce-bfc1-08002be10318}"), // Mouse
            new RawEnumEntry("HID", @"HID\VID_046D&PID_C52B&MI_01\7&1a2b3c&0&0000",
                "USB 输入设备", "{745a17a0-74d3-11d0-b6fe-00a0c90f57da}"), // HIDClass → HID 设备
            new RawEnumEntry("USB", @"USB\VID_1A86&PID_7523\6&2f9d&0&1",
                null, "{36fc9e60-c465-11cf-8056-444553540000}"), // Net 类 → 过滤掉
            new RawEnumEntry("USBSTOR", @"USBSTOR\Disk&Ven_Generic&Prod_SD_USB\...",
                "Generic SD USB Device", "{4d36e967-e325-11ce-bfc1-08002be10318}"), // DiskDrive → 过滤
            new RawEnumEntry("USB", @"USB\VID_046D&PID_C08B\5&2d3f&0&2",
                "@oem39.inf,%devicedesc%;Logitech G502 HERO Gaming Mouse", "{4d36e96f-e325-11ce-bfc1-08002be10318}") // 重复
        };

        var list = PeripheralParser.Parse(entries);

        Assert.Equal(2, list.Count);
        Assert.Equal("Mouse", list[0].Kind);
        Assert.Equal("Logitech G502 HERO Gaming Mouse", list[0].Name); // 前缀已剥离
        Assert.Equal("HID 设备", list[1].Kind);
    }
}
```

- [ ] **Step 2: 运行确认失败**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~PeripheralParserTests
```

Expected: 编译失败。

- [ ] **Step 3: 实现**

`src/HardwareBench.Core/Detection/Peripheral/PeripheralParser.cs`：

```csharp
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection.Peripheral;

public sealed record RawEnumEntry(
    string Branch, string InstancePath, string? DeviceDesc, string? ClassGuid);

public static class PeripheralParser
{
    private static readonly (string Guid, string Kind)[] KnownClasses =
    [
        ("{4d36e96f-e325-11ce-bfc1-08002be10318}", "Mouse"),
        ("{4d36e96b-e325-11ce-bfc1-08002be10318}", "Keyboard"),
        ("{4d36e96c-e325-11ce-bfc1-08002be10318}", "Media"),
        ("{745a17a0-74d3-11d0-b6fe-00a0c90f57da}", "HID 设备"),
        ("{6d807884-7d21-11cf-801e-08002be10318}", "Printer"),
    ];

    public static List<PeripheralInfo> Parse(IEnumerable<RawEnumEntry> entries)
    {
        var result = new List<PeripheralInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            var kind = KnownClasses
                .FirstOrDefault(c => string.Equals(c.Guid, e.ClassGuid, StringComparison.OrdinalIgnoreCase)).Kind;
            if (kind is null) continue;
            if (!seen.Add(e.InstancePath)) continue;
            string name = CleanName(e.DeviceDesc) ?? e.InstancePath;
            result.Add(new PeripheralInfo(kind, name, e.InstancePath));
        }
        return result;
    }

    /// <summary>"@oem39.inf,%key%;显示名" → "显示名"；普通字符串原样。</summary>
    internal static string? CleanName(string? raw)
    {
        if (raw is null) return null;
        int semi = raw.IndexOf(';');
        return raw.Length > 0 && raw[0] == '@' && semi >= 0 ? raw[(semi + 1)..] : raw;
    }
}
```

`src/HardwareBench.Core/Detection/Peripheral/RegistryPeripheralSource.cs`（薄层，不单测）：

```csharp
using Microsoft.Win32;

namespace HardwareBench.Core.Detection.Peripheral;

/// <summary>枚举 HKLM\SYSTEM\CurrentControlSet\Enum 下 USB/HID/USBSTOR 分支。</summary>
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
```

（解析函数消费 `IEnumerable<RawEnumEntry>`，薄层生产之——检测器组装放 Task 8/9。）

- [ ] **Step 4: 运行确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~PeripheralParserTests
```

Expected: 1 passed。

- [ ] **Step 5: Commit**

```powershell
# workdir: REPO_ROOT
git add src
git commit -m "feat: peripheral enumeration with registry indirect-name cleanup"
```

---

### Task 7: WMI 清单检测器（Hardware.Info 封装）

**Files:**
- Create: `src/HardwareBench.Core/Detection/Wmi/IWmiSource.cs`, `src/HardwareBench.Core/Detection/Wmi/HardwareInfoWmiSource.cs`, `src/HardwareBench.Core/Detection/Wmi/WmiInventoryDetector.cs`
- Test: `src/HardwareBench.Tests/Detection/WmiInventoryDetectorTests.cs`
- Modify: `src/HardwareBench.Core/HardwareBench.Core.csproj`（加包引用）

**Interfaces:**
- Consumes: Hardware.Info 的 `Cpu/Motherboard/Memory/VideoController/Drive/OperatingSystem` 类型；Task 2 模型
- Produces:
  - `interface IWmiSource { IEnumerable<Cpu> GetCpuList(); IEnumerable<Motherboard> GetMotherboardList(); IEnumerable<Memory> GetMemoryList(); IEnumerable<VideoController> GetVideoControllerList(); IEnumerable<Drive> GetDriveList(); IEnumerable<OperatingSystem> GetOperatingSystemList(); }`
  - `class WmiInventoryDetector(IWmiSource source) { string Id => "wmi.inventory"; Task DetectAsync(HardwareReport, CancellationToken); }`——每个分区独立 try/catch，失败写入 `report.Errors`

- [ ] **Step 1: 加 NuGet 包**

```powershell
# workdir: REPO_ROOT\src\HardwareBench.Core
dotnet add package Hardware.Info
```

- [ ] **Step 2: 写失败测试**

`src/HardwareBench.Tests/Detection/WmiInventoryDetectorTests.cs`：

```csharp
using HardwareBench.Core.Detection.Wmi;
using HardwareBench.Core.Models;
using Hardware.Info;

namespace HardwareBench.Tests.Detection;

public class WmiInventoryDetectorTests
{
    private sealed class FakeSource : IWmiSource
    {
        public List<Cpu> Cpus { get; } = [];
        public List<Motherboard> Boards { get; } = [];
        public List<Memory> Memory { get; } = [];
        public List<VideoController> Gpus { get; } = [];
        public List<Drive> Drives { get; } = [];
        public List<OperatingSystem> Os { get; } = [];
        public bool ThrowCpu { get; set; }
        public bool ThrowMemory { get; set; }

        public IEnumerable<Cpu> GetCpuList() { if (ThrowCpu) throw new InvalidOperationException("wmi down"); return Cpus; }
        public IEnumerable<Motherboard> GetMotherboardList() => Boards;
        public IEnumerable<Memory> GetMemoryList() { if (ThrowMemory) throw new InvalidOperationException("wmi down"); return Memory; }
        public IEnumerable<VideoController> GetVideoControllerList() => Gpus;
        public IEnumerable<Drive> GetDriveList() => Drives;
        public IEnumerable<OperatingSystem> GetOperatingSystemList() => Os;
    }

    [Fact]
    public async Task Detect_MapsAllSections()
    {
        var src = new FakeSource
        {
            Cpus = { new Cpu { Name = "Intel Core i7-13700K", NumberOfCores = 16, NumberOfLogicalProcessors = 24, MaxClockSpeed = 5400 } },
            Boards = { new Motherboard { Manufacturer = "ASUSTeK", Product = "ROG STRIX Z790-E" } },
            Memory = { new Memory { BankLabel = "BANK 0", DeviceLocator = "ChannelA-DIMM1", Capacity = 34359738368, Speed = 5600 } },
            Gpus = { new VideoController { Name = "NVIDIA GeForce RTX 4070", DriverVersion = "32.0.15.6094" } },
            Os = { new OperatingSystem { Name = "Microsoft Windows 11 Pro", Version = "10.0.26100", BuildNumber = "26100", OSArchitecture = "64-bit" } }
        };
        var report = new HardwareReport();

        await new WmiInventoryDetector(src).DetectAsync(report, CancellationToken.None);

        Assert.NotNull(report.Os);
        Assert.Equal("Intel Core i7-13700K", report.Cpu!.Name);
        Assert.Equal(24, report.Cpu.LogicalProcessors);
        Assert.Equal("ROG STRIX Z790-E", report.Motherboard!.Product);
        Assert.Single(report.MemoryModules);
        Assert.Equal(34359738368ul, report.MemoryModules[0].CapacityBytes);
        Assert.Equal("NVIDIA GeForce RTX 4070", report.Gpus[0].Name);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task Detect_SectionFailure_RecordsErrorAndContinues()
    {
        var src = new FakeSource { ThrowCpu = true, ThrowMemory = true };
        var report = new HardwareReport();

        await new WmiInventoryDetector(src).DetectAsync(report, CancellationToken.None);

        Assert.Null(report.Cpu);
        Assert.Empty(report.MemoryModules);
        Assert.Equal(2, report.Errors.Count);
        Assert.Contains(report.Errors, e => e.DetectorId == "wmi.inventory");
    }
}
```

- [ ] **Step 3: 运行确认失败（属性名与 lib 实际不符时按 lib 调整断言，映射意图以测试注释为准）**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~WmiInventoryDetectorTests
```

Expected: 编译失败（IWmiSource 不存在）。若 Hardware.Info 的属性名与上述 fixture 不同（例如 `OperatingSystem.BuildNumber` 实际为其他名），修正 fixture 与映射使其与库实际类型一致——映射意图不变。

- [ ] **Step 4: 实现**

`src/HardwareBench.Core/Detection/Wmi/IWmiSource.cs`：

```csharp
using Hardware.Info;

namespace HardwareBench.Core.Detection.Wmi;

public interface IWmiSource
{
    IEnumerable<Cpu> GetCpuList();
    IEnumerable<Motherboard> GetMotherboardList();
    IEnumerable<Memory> GetMemoryList();
    IEnumerable<VideoController> GetVideoControllerList();
    IEnumerable<Drive> GetDriveList();
    IEnumerable<OperatingSystem> GetOperatingSystemList();
}
```

`src/HardwareBench.Core/Detection/Wmi/HardwareInfoWmiSource.cs`（薄层，不单测；每个方法只调 HardwareInfo 对应查询）：

```csharp
using Hardware.Info;

namespace HardwareBench.Core.Detection.Wmi;

public sealed class HardwareInfoWmiSource : IWmiSource, IDisposable
{
    private readonly HardwareInfo _hw = new();

    public IEnumerable<Cpu> GetCpuList() { _hw.RefreshCpuList(); return _hw.CpuList; }
    public IEnumerable<Motherboard> GetMotherboardList() { _hw.RefreshMotherboardList(); return _hw.MotherboardList; }
    public IEnumerable<Memory> GetMemoryList() { _hw.RefreshMemoryList(); return _hw.MemoryList; }
    public IEnumerable<VideoController> GetVideoControllerList() { _hw.RefreshVideoControllerList(); return _hw.VideoControllerList; }
    public IEnumerable<Drive> GetDriveList() { _hw.RefreshDriveList(); return _hw.DriveList; }
    public IEnumerable<OperatingSystem> GetOperatingSystemList() { _hw.RefreshOperatingSystemList(); return _hw.OperatingSystem; }

    public void Dispose() => _hw.Dispose();
}
```

（Hardware.Info 的实际方法/属性名若不同，以库的 IntelliSense/README 为准调整——保持「一个查询一个方法」结构。）

`src/HardwareBench.Core/Detection/Wmi/WmiInventoryDetector.cs`：

```csharp
using HardwareBench.Core.Models;
using Hardware.Info;

namespace HardwareBench.Core.Detection.Wmi;

public sealed class WmiInventoryDetector(IWmiSource source)
{
    public string Id => "wmi.inventory";

    public Task DetectAsync(HardwareReport report, CancellationToken ct)
    {
        Safe(report, ct, r =>
        {
            var cpu = source.GetCpuList().FirstOrDefault();
            if (cpu is null) return;
            r.Cpu = new CpuInfo(cpu.Name ?? "未知 CPU", cpu.ProcessorId,
                cpu.NumberOfCores, cpu.NumberOfLogicalProcessors,
                cpu.MaxClockSpeed, cpu.L3CacheSize > 0 ? cpu.L3CacheSize : null);
        });
        Safe(report, ct, r =>
        {
            var board = source.GetMotherboardList().FirstOrDefault();
            if (board is not null)
                r.Motherboard = new MotherboardInfo(
                    board.Manufacturer ?? "未知", board.Product ?? "未知", board.SerialNumber);
        });
        Safe(report, ct, r =>
        {
            foreach (var m in source.GetMemoryList())
                r.MemoryModules.Add(new MemoryModule(
                    m.BankLabel ?? "", m.DeviceLocator ?? "", m.Capacity,
                    m.Speed, Clean(m.Manufacturer), Clean(m.PartNumber), Clean(m.SerialNumber), null));
        });
        Safe(report, ct, r =>
        {
            foreach (var v in source.GetVideoControllerList())
                r.Gpus.Add(new GpuInfo(v.Name ?? "未知显卡", v.AdapterCompatibility,
                    v.AdapterRAM == 0 ? null : v.AdapterRAM, v.DriverVersion, v.VideoProcessor));
        });
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static void Safe(HardwareReport r, CancellationToken ct, Action<HardwareReport> section)
    {
        try { ct.ThrowIfCancellationRequested(); section(r); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { r.Errors.Add(new DetectionError("wmi.inventory", ex.Message)); }
    }

    private static string? Clean(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
```

- [ ] **Step 5: 运行确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~WmiInventoryDetectorTests
```

Expected: 2 passed。

- [ ] **Step 6: Commit**

```powershell
# workdir: REPO_ROOT
git add src
git commit -m "feat: WMI inventory detector over Hardware.Info"
```

---

### Task 8: DetectionService 聚合服务

**Files:**
- Create: `src/HardwareBench.Core/Detection/IHardwareDetector.cs`, `src/HardwareBench.Core/Detection/DetectionService.cs`
- Test: `src/HardwareBench.Tests/Detection/DetectionServiceTests.cs`

**Interfaces:**
- Consumes: Task 4/5/7 的三个检测器（本任务只依赖统一接口，不依赖具体类）
- Produces:
  - `interface IHardwareDetector { string Id { get; Task DetectAsync(HardwareReport report, CancellationToken ct); } }`
  - `interface IDetectionService { Task<HardwareReport> DetectAsync(CancellationToken ct); }`
  - `class DetectionService : IDetectionService`——顺序执行、per-detector 容错（异常 → `Errors`）、`CapturedAtUtc` 由服务填写
- 重构：MonitorDetector/StorageDetector/WmiInventoryDetector 补 `: IHardwareDetector` 接口声明（签名已一致，只加接口）

- [ ] **Step 1: 写失败测试**

`src/HardwareBench.Tests/Detection/DetectionServiceTests.cs`：

```csharp
using HardwareBench.Core.Detection;
using HardwareBench.Core.Models;

namespace HardwareBench.Tests.Detection;

public class DetectionServiceTests
{
    private sealed class FakeDetector : IHardwareDetector
    {
        public string Id { get; }
        private readonly Action<HardwareReport> _act;
        public FakeDetector(string id, Action<HardwareReport> act) { Id = id; _act = act; }
        public Task DetectAsync(HardwareReport report, CancellationToken ct) { _act(report); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Detect_RunsAll_FaultTolerant_AndStampsTime()
    {
        var before = DateTimeOffset.UtcNow;
        var service = new DetectionService(new IHardwareDetector[]
        {
            new FakeDetector("cpu.ok", r => r.Cpu = new CpuInfo("i7", null, 8, 16, 5000, null)),
            new FakeDetector("bad.one", _ => throw new IOException("boom")),
            new FakeDetector("gpu.ok", r => r.Gpus.Add(new GpuInfo("RTX", null, null, null, null)))
        });

        var report = await service.DetectAsync(CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal("i7", report.Cpu!.Name);
        Assert.Single(report.Gpus);
        var err = Assert.Single(report.Errors);
        Assert.Equal("bad.one", err.DetectorId);
        Assert.Equal("boom", err.Message);
        Assert.InRange(report.CapturedAtUtc, before, after);
    }

    [Fact]
    public async Task Detect_Cancellation_Propagates()
    {
        var cts = new CancellationTokenSource(canceled: true);
        var service = new DetectionService(
            [new FakeDetector("x", _ => { })]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.DetectAsync(cts.Token));
    }
}
```

- [ ] **Step 2: 运行确认失败**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~DetectionServiceTests
```

Expected: 编译失败。

- [ ] **Step 3: 实现接口与服务**

`src/HardwareBench.Core/Detection/IHardwareDetector.cs`：

```csharp
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection;

public interface IHardwareDetector
{
    string Id { get; }
    Task DetectAsync(HardwareReport report, CancellationToken ct);
}

public interface IDetectionService
{
    Task<HardwareReport> DetectAsync(CancellationToken ct);
}
```

`src/HardwareBench.Core/Detection/DetectionService.cs`：

```csharp
using HardwareBench.Core.Models;

namespace HardwareBench.Core.Detection;

public sealed class DetectionService(IEnumerable<IHardwareDetector> detectors) : IDetectionService
{
    public async Task<HardwareReport> DetectAsync(CancellationToken ct)
    {
        var report = new HardwareReport { CapturedAtUtc = DateTimeOffset.UtcNow };
        foreach (var detector in detectors)
        {
            ct.ThrowIfCancellationRequested();
            try { await detector.DetectAsync(report, ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { report.Errors.Add(new DetectionError(detector.Id, ex.Message)); }
        }
        return report;
    }
}
```

给三个检测器类声明加上 `: IHardwareDetector`（例如 `public sealed class MonitorDetector(IEdidSource source) : IHardwareDetector`），并删除 WmiInventoryDetector 内部重复的 Safe 帮助逻辑对其 public 契约无影响（保留）。

- [ ] **Step 4: 运行全量测试确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests
```

Expected: 全部通过（此前所有任务测试 + 新增 2）。

- [ ] **Step 5: Commit**

```powershell
# workdir: REPO_ROOT
git add src
git commit -m "feat: DetectionService with per-detector fault tolerance"
```

---

### Task 9: WPF 外壳 + 检测页 ViewModel

**Files:**
- Create: `src/HardwareBench.App/Strings.zh-CN.xaml`, `src/HardwareBench.App/ViewModels/DetectionViewModel.cs`, `src/HardwareBench.App/Views/DetectionPage.xaml(.cs)`, `src/HardwareBench.App/Views/PlaceholderPage.xaml(.cs)`
- Modify: `src/HardwareBench.App/App.xaml(.cs)`, `src/HardwareBench.App/MainWindow.xaml(.cs)`, `src/HardwareBench.App/HardwareBench.App.csproj`（包引用）, `src/HardwareBench.Tests/HardwareBench.Tests.csproj`（WPF 支持，仅用于 VM 测试无需——纯 VM 测试不需要 WPF 工程）
- Test: `src/HardwareBench.Tests/App/DetectionViewModelTests.cs`

**Interfaces:**
- Consumes: `IDetectionService`（Task 8）、`ReportJson`（Task 2）
- Produces:
  - `class DetectionViewModel : ObservableObject`，`[RelayCommand] LoadAsync()`、`[RelayCommand] ExportAsync()`；属性 `IsBusy:bool`、`Report:HardwareReport?`、`StatusText:string`、`HasReport:bool`（Report != null）
  - `interface IFileSaveService { string? PickSavePath(string defaultName); }`（VM 导出用，UI 层实现 SaveFileDialog；测试用 fake）

- [ ] **Step 1: 加包引用**

```powershell
# workdir: REPO_ROOT\src\HardwareBench.App
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Extensions.Hosting
```

- [ ] **Step 2: 写失败测试（VM 纯逻辑，无需 STA）**

`src/HardwareBench.Tests/App/DetectionViewModelTests.cs`：

```csharp
using HardwareBench.Core.Detection;
using HardwareBench.Core.Models;
using HardwareBench.App.ViewModels;

namespace HardwareBench.Tests.App;

public class DetectionViewModelTests
{
    private sealed class FakeService : IDetectionService
    {
        public Task<HardwareReport> DetectAsync(CancellationToken ct) => Task.FromResult(new HardwareReport
        {
            Cpu = new CpuInfo("i7-13700K", null, 16, 24, 5400, null),
            Errors = { new DetectionError("x.y", "boom") }
        });
    }

    private sealed class FakeSaveService : IFileSaveService
    {
        public string? SavedContent { get; private set; }
        public string? PickSavePath(string defaultName) => Path.Combine(Path.GetTempPath(), defaultName);
        public void Capture(string content) => SavedContent = content;
    }

    // IFileSaveService 定义为：string? PickSavePath(string defaultName, out TextWriter? writer);
    // 若采用此形态，Fake 直接开 StringWriter。二选一，保持接口最小：见 Step 3 定义。

    [Fact]
    public async Task Load_PopulatesReportAndStatus()
    {
        var vm = new DetectionViewModel(new FakeService(), new FakeExporter());

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.IsBusy);
        Assert.NotNull(vm.Report);
        Assert.Equal("i7-13700K", vm.Report!.Cpu!.Name);
        Assert.Contains("1", vm.StatusText); // 1 个分项失败
    }

    [Fact]
    public async Task Export_WritesJsonToChosenPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hb-test-{Guid.NewGuid():N}.json");
        var exporter = new FakeExporter { PathToReturn = path };
        var vm = new DetectionViewModel(new FakeService(), exporter);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.True(File.Exists(path));
        var json = File.ReadAllText(path);
        Assert.Contains("i7-13700K", json);
        File.Delete(path);
    }

    private sealed class FakeExporter : IFileSaveService
    {
        public string? PathToReturn { get; set; }
        public string? PickSavePath(string defaultName) => PathToReturn ?? Path.Combine(Path.GetTempPath(), defaultName);
    }
}
```

- [ ] **Step 3: 运行确认失败**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~DetectionViewModelTests
```

Expected: 编译失败。

- [ ] **Step 4: 实现 ViewModel**

接口（放 `src/HardwareBench.App/ViewModels/IFileSaveService.cs`）：

```csharp
namespace HardwareBench.App.ViewModels;

public interface IFileSaveService
{
    /// <summary>弹保存对话框；用户取消返回 null。</summary>
    string? PickSavePath(string defaultName);
}
```

`src/HardwareBench.App/ViewModels/DetectionViewModel.cs`：

```csharp
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HardwareBench.Core.Detection;
using HardwareBench.Core.Serialization;

namespace HardwareBench.App.ViewModels;

public partial class DetectionViewModel(
    IDetectionService detectionService, IFileSaveService fileSave) : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private HardwareReport? _report;
    [ObservableProperty] private string _statusText = "就绪——点击「开始检测」";

    public bool HasReport => Report is not null;

    partial void OnReportChanged(HardwareReport? value)
        => OnPropertyChanged(nameof(HasReport));

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusText = "检测中……";
        try
        {
            Report = await detectionService.DetectAsync(CancellationToken.None);
            StatusText = Report.Errors.Count == 0
                ? "检测完成，无分项失败"
                : $"检测完成，{Report.Errors.Count} 个分项失败（详见列表）";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task ExportAsync()
    {
        if (Report is null) return Task.CompletedTask;
        var path = fileSave.PickSavePath($"HardwareReport-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        if (path is null) return Task.CompletedTask;
        File.WriteAllText(path, ReportJson.Serialize(Report));
        StatusText = $"已导出：{path}";
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5: 运行确认通过**

```powershell
# workdir: REPO_ROOT\src
dotnet test HardwareBench.Tests --filter FullyQualifiedName~DetectionViewModelTests
```

Expected: 2 passed。

- [ ] **Step 6: 实现 UI 外壳（手动 QA 项）**

`Strings.zh-CN.xaml`（ResourceDictionary，App.xaml 合并）：

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <sys:String x:Key="App.Title">硬件体检台</sys:String>
    <sys:String x:Key="Nav.Detection">硬件检测</sys:String>
    <sys:String x:Key="Nav.Benchmark">性能跑分</sys:String>
    <sys:String x:Key="Nav.History">历史结果</sys:String>
    <sys:String x:Key="Detection.Start">开始检测</sys:String>
    <sys:String x:Key="Detection.Export">导出 JSON</sys:String>
    <sys:String x:Key="Detection.Sections">检测报告</sys:String>
    <sys:String x:Key="Placeholder.Benchmark">性能跑分将在 M2 里程碑启用</sys:String>
    <sys:String x:Key="Placeholder.History">历史结果将在 M3 里程碑启用</sys:String>
</ResourceDictionary>
```

`App.xaml.cs`（Generic Host + DI + 导航 VM）：

```csharp
using System.Windows;
using HardwareBench.App.ViewModels;
using HardwareBench.App.Views;
using HardwareBench.Core.Detection;
using HardwareBench.Core.Detection.Monitor;
using HardwareBench.Core.Detection.Peripheral;
using HardwareBench.Core.Detection.Storage;
using HardwareBench.Core.Detection.Wmi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace HardwareBench.App;

public partial class App : Application
{
    private readonly IHost _host = Host.CreateApplicationBuilder(args).Build();

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = _host.Services;
        services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        var b = Host.CreateApplicationBuilder();
        b.Services.AddSingleton<IWmiSource, HardwareInfoWmiSource>();
        b.Services.AddSingleton<IEdidSource, RegistryEdidSource>();
        b.Services.AddSingleton<NativeStorageApi>();
        b.Services.AddSingleton<RegistryPeripheralSource>();
        b.Services.AddSingleton<IHardwareDetector, WmiInventoryDetector>();
        b.Services.AddSingleton<IHardwareDetector, StorageDetector>();
        b.Services.AddSingleton<IHardwareDetector, MonitorDetector>();
        b.Services.AddSingleton<IHardwareDetector, PeripheralRegistryDetector>();
        b.Services.AddSingleton<IDetectionService, DetectionService>();
        b.Services.AddSingleton<IFileSaveService, DialogFileSaveService>();
        b.Services.AddSingleton<MainViewModel>();
        b.Services.AddSingleton<MainWindow>();
        _host = b.Build();
        Services = _host.Services;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host.Dispose();
        base.OnExit(e);
    }
}

public sealed class DialogFileSaveService : IFileSaveService
{
    public string? PickSavePath(string defaultName)
    {
        var dlg = new SaveFileDialog { Filter = "JSON 文件|*.json", FileName = defaultName };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
```

（注：`PeripheralRegistryDetector` 为薄适配器——包 `RegistryPeripheralSource`+`PeripheralParser` 实现 `IHardwareDetector`，约 15 行，写在这个文件或独立文件均可：

```csharp
public sealed class PeripheralRegistryDetector(RegistryPeripheralSource source) : IHardwareDetector
{
    public string Id => "peripheral.registry";
    public Task DetectAsync(HardwareReport report, CancellationToken ct)
    {
        report.Peripherals.AddRange(PeripheralParser.Parse(source.ReadAll()));
        return Task.CompletedTask;
    }
}
```

`MainViewModel`（侧边栏导航 + 三页 VM 持有）：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace HardwareBench.App.ViewModels;

public partial class MainViewModel(
    DetectionViewModel detection,
    PlaceholderViewModel benchmark,
    PlaceholderViewModel history) : ObservableObject
{
    public IReadOnlyList<NavEntry> Nav { get; } =
    [
        new NavEntry("硬件检测", detection),
        new NavEntry("性能跑分", benchmark),
        new NavEntry("历史结果", history)
    ];

    [ObservableProperty] private ObservableObject? _currentPage = detection; // 构造后由生成器赋初值

    public sealed record NavEntry(string Title, ObservableObject ViewModel);
}

public partial class PlaceholderViewModel(string text) : ObservableObject
{
    public string Text { get; } = text;
}
```

`MainWindow.xaml`（侧边栏 + ContentControl 导航，DataTemplates 映射 VM→Page）：

```xml
<Window x:Class="HardwareBench.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:HardwareBench.App.ViewModels"
        xmlns:views="clr-namespace:HardwareBench.App.Views"
        Title="{DynamicResource App.Title}" Width="1100" Height="720"
        WindowStartupLocation="CenterScreen">
    <Window.DataContext>
        <vm:MainViewModel x:Name="Vm" />
    </Window.DataContext>
    <Window.Resources>
        <DataTemplate DataType="{x:Type vm:DetectionViewModel}">
            <views:DetectionPage/>
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:PlaceholderViewModel}">
            <views:PlaceholderPage/>
        </DataTemplate>
    </Window.Resources>
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="200"/>
            <ColumnDefinition/>
        </Grid.ColumnDefinitions>
        <ListBox ItemsSource="{Binding Nav}" SelectedValuePath="ViewModel"
                 SelectedValue="{Binding CurrentPage}" BorderThickness="0" Background="#F3F4F6">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Title}" Margin="14,10" FontSize="15"/>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
        <ContentControl Grid.Column="1" Content="{Binding CurrentPage}" Margin="16"/>
    </Grid>
</Window>
```

`MainWindow.xaml.cs`：默认极简（InitializeComponent + DataContext 从 `App.Services` 取 `MainViewModel`；移除 XAML 里的 `Window.DataContext`，改 code-behind `DataContext = App.Services.GetRequiredService<MainViewModel>();`——DI 优先，避免嵌套构造）。

`Views/DetectionPage.xaml`（绑定 DetectionViewModel）：

```xml
<UserControl x:Class="HardwareBench.App.Views.DetectionPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <DockPanel>
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,12">
            <Button Content="开始检测" Command="{Binding LoadCommand}"
                    Padding="16,6" FontWeight="Bold" IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}"/>
            <Button Content="导出 JSON" Command="{Binding ExportCommand}" Margin="8,0,0,0" Padding="12,6"
                    IsEnabled="{Binding HasReport}"/>
            <TextBlock Text="{Binding StatusText}" VerticalAlignment="Center" Margin="16,0,0,0"/>
        </StackPanel>
        <ProgressBar DockPanel.Dock="Top" IsIndeterminate="{Binding IsBusy}" Height="4"/>
        <ScrollViewer>
            <StackPanel Margin="0,12,0,0">
                <TextBlock Text="处理器" FontWeight="Bold" FontSize="16"/>
                <TextBlock Text="{Binding Report.Cpu.Name}" Margin="0,4" FontSize="14"/>
                <TextBlock Text="主板" FontWeight="Bold" FontSize="16" Margin="0,12,0,4"/>
                <TextBlock Text="{Binding Report.Motherboard.Manufacturer, StringFormat={}{0} {1}}"/>
                <TextBlock Text="内存" FontWeight="Bold" FontSize="16" Margin="0,12,0,4"/>
                <ItemsControl ItemsSource="{Binding Report.MemoryModules}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <TextBlock>
                                <Run Text="{Binding DeviceLocator}"/>
                                <Run Text=" · "/>
                                <Run Text="{Binding CapacityBytes, StringFormat={}{0:N0} B}"/>
                                <Run Text=" · "/>
                                <Run Text="{Binding SpeedMts, StringFormat={}{0} MT/s}"/>
                            </TextBlock>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <TextBlock Text="显卡" FontWeight="Bold" FontSize="16" Margin="0,12,0,4"/>
                <ItemsControl ItemsSource="{Binding Report.Gpus}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate><TextBlock Text="{Binding Name}"/></DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <TextBlock Text="存储" FontWeight="Bold" FontSize="16" Margin="0,12,0,4"/>
                <ItemsControl ItemsSource="{Binding Report.Storage}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <TextBlock>
                                <Run Text="{Binding Model}"/><Run Text=" · "/>
                                <Run Text="{Binding BusType}"/><Run Text=" · "/>
                                <Run Text="{Binding TemperatureC, StringFormat={}{0} ℃, TargetNullValue=温度未知}"/>
                            </TextBlock>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <TextBlock Text="显示器" FontWeight="Bold" FontSize="16" Margin="0,12,0,4"/>
                <ItemsControl ItemsSource="{Binding Report.Monitors}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <TextBlock>
                                <Run Text="{Binding ModelName, TargetNullValue=(未提供名称)}"/>
                                <Run Text=" · "/><Run Text="{Binding ManufacturerId}"/>
                            </TextBlock>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <TextBlock Text="外设" FontWeight="Bold" FontSize="16" Margin="0,12,0,4"/>
                <ItemsControl ItemsSource="{Binding Report.Peripherals}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <TextBlock>
                                <Run Text="["><Run Text="{Binding Kind}"/><Run Text="] "/>
                                <Run Text="{Binding Name}"/>
                            </TextBlock>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </ScrollViewer>
    </DockPanel>
</UserControl>
```

（需要的 `InverseBool` 转换器：App.xaml 资源里加一个 12 行的 `BooleanToInverseVisibilityConverter` 或用 `IsEnabled="{Binding !IsBusy}"`——CommunityToolkit 支持 `!` 绑定语法需 `x:Bind` 不适用 WPF；直接写一个 IValueConverter 放 Strings.zh-CN.xaml 同目录 `Converters.cs`。）

`Views/PlaceholderPage.xaml`：TextBlock 绑定 `{Binding Text}` 居中即可。`DetectionPage.xaml.cs`/`PlaceholderPage.xaml.cs`：仅 InitializeComponent。

- [ ] **Step 7: 手动 QA（真实表面验证）**

```powershell
# workdir: REPO_ROOT\src
dotnet build HardwareBench.sln -c Debug
dotnet run --project HardwareBench.App
```

检查清单（截图留证到 `docs/qa/m1/`）：
1. 启动 → 三页导航可切换
2. 点「开始检测」→ 进度条动、几秒后状态栏显示完成
3. CPU/主板/内存/显卡/存储/显示器/外设各节显示真机数据（与设备管理器抽查一致）
4. 「导出 JSON」→ 保存的文件可被 `ReportJson.Deserialize` 读回（用临时 xunit 或 dotnet-script 验证）
5. 无任何弹窗报错；若有 Errors 列表显示在状态栏计数

- [ ] **Step 8: Commit**

```powershell
# workdir: REPO_ROOT
git add src docs
git commit -m "feat: WPF shell with detection page and JSON export"
```

---

### Task 10: 便携发布 + 文档 + 双远端

**Files:**
- Create: `publish.ps1`, `THIRDPARTY-NOTICES.md`, `README.md`

**Interfaces:**
- Consumes: 全部
- Produces: `publish/HardwareBench.exe`（单文件自包含）

- [ ] **Step 1: publish.ps1**

```powershell
# 单文件自包含发布（win-x64）
param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
dotnet publish "$PSScriptRoot\src\HardwareBench.App" -c $Configuration -r win-x64 `
    --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true `
    -o "$PSScriptRoot\publish"
Get-Item "$PSScriptRoot\publish\HardwareBench.exe" | Select-Object FullName, @{n="SizeMB";e={[math]::Round($_.Length/1MB,1)}}
Get-FileHash "$PSScriptRoot\publish\HardwareBench.exe" -Algorithm SHA256 | Format-List
```

- [ ] **Step 2: THIRDPARTY-NOTICES.md**

```markdown
# 第三方组件声明

| 组件 | 许可证 | 用途 |
|---|---|---|
| Hardware.Info (Jinjinov) | MIT | WMI 硬件信息查询 |
| CommunityToolkit.Mvvm | MIT | MVVM 基础设施 |
| Microsoft.Extensions.Hosting | MIT | 依赖注入容器 |
| .NET 运行时与 WPF | MIT | 运行时框架 |

完整许可文本见各组件发行包。
```

- [ ] **Step 3: README.md**

包含：项目简介（轻量/客观/无广告定位）、截图位、构建方法（`.\publish.ps1`）、路线图（M1 已交付检测 / M2 跑分闭环 / M3 GPU+历史+传感器）、客观性承诺（物理值优先、公开参照表、几何平均——链接 design.md）、许可证 MIT、THIRDPARTY-NOTICES 链接。

- [ ] **Step 4: 发布验证（真实表面）**

```powershell
# workdir: REPO_ROOT
.\publish.ps1
.\publish\HardwareBench.exe   # 手动：双击运行跑一遍检测+导出，截图
```

Expected: 单 exe（预计 40–60MB）；SHA256 输出记录进 README 的 Release 段落。

- [ ] **Step 5: 打 tag 并提交**

```powershell
# workdir: REPO_ROOT
git add README.md THIRDPARTY-NOTICES.md publish.ps1
git commit -m "docs: readme, third-party notices, portable publish script"
git tag v0.1.0-m1
```

- [ ] **Step 6: 双远端推送（需老板提供仓库地址）**

```powershell
# workdir: REPO_ROOT（URL 由老板提供后替换）
git remote add github https://github.com/<老板账号>/HardwareBench.git
git remote add gitee  https://gitee.com/<老板账号>/HardwareBench.git
git push github main --tags
git push gitee main --tags
```

此步骤执行前向老板索取两个仓库 URL（或由老板在 GitHub/Gitee 建好空仓库后提供）。

---

## 自审记录（Self-Review）

1. **Spec 覆盖**：design.md §4.1 检测器表格逐行对应——WMI 清单(Task 7)、EDID(Task 3/4)、SMART→IOCTL 描述符(Task 5，范围微调已在 Global Constraints 声明)、外设(Task 6)、传感器=不做(M3)；§4.5 JSON 导出(Task 2/9)；§6 错误处理 WMI 单项失败(Task 7 Safe + Task 8)；§7 测试策略(各任务 TDD)；便携发布(Task 10)。UI 三页(Task 9，跑分/历史为占位文案——是 UI 文案不是计划占位)。
2. **占位符扫描**：无 TBD/TODO；P/Invoke 常量附「对照 winioctl.h 核对」验证步骤，属实现核对动作而非占位。
3. **类型一致性**：`IHardwareDetector.DetectAsync(HardwareReport, CancellationToken)` 在 Task 4/5/7/8 一致；`PeripheralInfo(Kind, Name, InstancePath)` 三参数在 Task 2/6 一致；`StorageDevice` 八参数在 Task 2/5 一致；`IFileSaveService.PickSavePath(string)` 在 Task 9 测试与实现一致（测试文件中曾注释另一形态，已统一为 `string? PickSavePath(string)`）。
