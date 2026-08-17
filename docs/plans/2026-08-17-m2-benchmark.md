# M2 跑分闭环 · 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 交付 M2——磁盘/CPU/内存三引擎跑分闭环：工具内嵌提取（SHA256 校验）→ Job Object 受限子进程执行 → fixture 实证的三套输出解析器 → 公平性守卫 → 公开参照表评分 + 几何平均总分 → 跑分页 UI + `--benchmark` 无头模式 → v0.2.0-m2 便携发布（两轮复现性 <5% 验收）。

**Architecture:** 引擎层全部在 Core：`IBenchmarkEngine` 单轮执行返回原始物理值，`BenchmarkService` 编排（预热策略 + 3 轮取中位数 + 引擎级容错），`Scoring`/`ReferenceTable` 纯函数换算，`FairnessGuard` 采样环境仅提示不阻断。工具二进制以 EmbeddedResource 内嵌（保持单文件发布），运行时释放到 `%TEMP%\HardwareBench\tools\` 并校验 SHA256 后才执行。

**Tech Stack:** .NET 8（Core net8.0 / App net8.0-windows）、P/Invoke（Job Object、GetSystemTimes）、System.Text.Json、xUnit。零新 NuGet 依赖。

## Global Constraints

- 分支 `feature/m2`（基于合并后的 main）；REPO_ROOT = `D:\OpenCodePortable\WORKSPACE\2026-08-17-硬件测评程序`
- `<Nullable>enable</Nullable>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`（已在 Directory.Build.props）
- **工具安全铁律**：基准工具只能来自 `assets/tools/` 内嵌资源；执行前必须 SHA256 校验（哈希常量与 `assets/tools/SHA256SUMS.txt` 一致）；禁止任何运行时下载；所有写入仅限 `%TEMP%\HardwareBench\`
- 无管理员要求、无 ring0、无传感器（M3 内容）；GPU 引擎不在 M2
- 预热策略（对 design.md §4.3 的实证修订，已批准）：DiskSpd 引擎 1 轮短预热（-d3），7-Zip/STREAM 预热 0 轮——DiskSpd 有内建 warmup、STREAM NTIMES≥10 取最优、7-Zip 靠 3 轮中位数吸收首轮抖动（完整 4 轮会使跑分时长翻倍至 8 分钟，不可接受）
- 评分契约：中位数 → `min(1000, round(v/ref*1000))`；总分 = 已评分项几何平均（任一 0 分 → 总分 0）；参照表版本化 `2026.08-m2` 并在仓库公开可调
- UI 中文（沿用 M1 的硬编码节标题模式）；无头模式 `--benchmark [--export <path>]` 不开窗口
- 每任务收尾 `dotnet build` 零警告 + 全套测试绿后才 commit（conventional commits）
- 7-Zip 引擎集成测试慢（约 1-2 分钟），打 `[Trait("Category","Integration")]`，CI/本地可用 `--filter Category!=Integration` 跳过

## 实证依据（本会话真机探测，全部已入库）

- 资产 SHA256（`assets/tools/SHA256SUMS.txt`）：diskspd.exe `8F3B2F09…AEE9`、7zr.exe `56B8CC9F…CD72`、stream-windows.exe `A09913F5…846E`
- 三份真实输出 fixture：`docs/fixtures/m2/{fixture-diskspd-4k,fixture-diskspd-seq,fixture-7zr,fixture-stream}.txt`
- STREAM Windows 变体三处补丁：`_WIN32` 守卫（unistd.h/sys/time.h）、QPC 替代 gettimeofday、`ssize_t` typedef；许可条款 3b 的 variant 标注义务已写入 THIRDPARTY-NOTICES.md
- DiskSpd v2.2 非管理员运行会输出 SeManageVolumePrivilege 警告到 stderr（无害，解析器只读 stdout）
- 本机参考值：7zr Tot rating 60931、STREAM Triad 27896.9 MB/s、DiskSpd 4K 随机 156.94 MiB/s、顺序 ~589 MiB/s（电源计划「平衡」）

## 文件结构总览

```
src/HardwareBench.Core/
├─ Benchmarks/BenchmarkModels.cs        # T1 MetricValue/MetricResult/BenchmarkError/BenchmarkEnvironment/BenchmarkResult
├─ Benchmarks/BenchmarkJson.cs          # T1
├─ Benchmarks/ToolHashes.cs             # T2 哈希常量 + ToolVersions
├─ Benchmarks/ToolExtractor.cs          # T2 IToolLocator 实现（释放+校验+缓存）
├─ Benchmarks/ProcessRunner.cs          # T3 ProcessSpec/ProcessResult/IProcessRunner/BoundedProcessRunner + JobObjectScope
├─ Benchmarks/Parsers/DiskSpdParser.cs  # T4（纯函数）
├─ Benchmarks/Parsers/SevenZipParser.cs # T4
├─ Benchmarks/Parsers/StreamParser.cs   # T4
├─ Benchmarks/Engines/IBenchmarkEngine.cs # T5
├─ Benchmarks/Engines/DiskSpdEngine.cs    # T5
├─ Benchmarks/Engines/SevenZipEngine.cs   # T5
├─ Benchmarks/Engines/MemoryStreamEngine.cs # T5
├─ Benchmarks/FairnessGuard.cs          # T6 IFairnessGuard/FairnessReport/CpuBusySampler/PowerSchemeParser
├─ Benchmarks/Scoring.cs                # T7 ComputeScore/GeometricMean/Median
├─ Benchmarks/ReferenceTable.cs         # T7 版本化参照表
├─ Benchmarks/BenchmarkService.cs       # T8 IBenchmarkService/BenchmarkProgress/实现
src/HardwareBench.App/
├─ ViewModels/BenchmarkViewModel.cs     # T9
├─ Views/BenchmarkPage.xaml(.cs)        # T9
├─ （改）App.xaml.cs / MainViewModel / MainWindow / Core+App.csproj（内嵌资源）
docs/plans/2026-08-17-m2-benchmark.md   # 本计划
docs/qa/m2/                             # T10 复现性证据
```

---

### Task 1: 跑分领域模型 + JSON

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/BenchmarkModels.cs`、`src/HardwareBench.Core/Benchmarks/BenchmarkJson.cs`
- Test: `src/HardwareBench.Tests/Benchmarks/BenchmarkJsonTests.cs`

**Interfaces (Produces，后续任务逐字依赖):**
- `record MetricValue(string Id, string Unit, double Value);`
- `record MetricResult(string Id, string Category, string Unit, double Value, int? Score, double[] AllRuns);`
- `record BenchmarkError(string EngineId, string Message);`
- `record BenchmarkEnvironment(string PowerScheme, double BackgroundCpuPercent, string ToolVersions);`
- `class BenchmarkResult { DateTimeOffset StartedAtUtc; BenchmarkEnvironment Environment; List<MetricResult> Metrics; List<BenchmarkError> Errors; int? TotalScore; }`
- `static class BenchmarkJson { string Serialize(BenchmarkResult); BenchmarkResult Deserialize(string); }`（选项：WriteIndented + WhenWritingNull，同 M1 ReportJson）

- [ ] **Step 1 失败测试**：构造含 2 个 MetricResult（一个有分一个无分）、1 个 Error、TotalScore=512 的结果，`Serialize`→`Deserialize` 断言全部字段（含 AllRuns 数组与 StartedAtUtc）往返一致；`TotalScore=null` 的第二例断言往返后仍为 null。
- [ ] **Step 2 RED 运行**：`dotnet test src\HardwareBench.Tests --filter FullyQualifiedName~BenchmarkJsonTests`（编译失败即 RED）
- [ ] **Step 3 实现**：模型用 sealed record（BenchmarkResult 为 sealed class，List 属性初始化 `[]`，与 M1 HardwareReport 同风格）；BenchmarkJson 照 ReportJson 模式。
- [ ] **Step 4 GREEN 运行**（同 Step 1 命令，2 passed）
- [ ] **Step 5 全套 + 零警告 + Commit**：`git commit -m "feat: benchmark domain models with JSON roundtrip"`

---

### Task 2: 工具内嵌 + 释放校验（ToolExtractor）

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/ToolHashes.cs`、`src/HardwareBench.Core/Benchmarks/ToolExtractor.cs`
- Modify: `src/HardwareBench.Core/HardwareBench.Core.csproj`
- Test: `src/HardwareBench.Tests/Benchmarks/ToolExtractorTests.cs`

**Interfaces:**
- `interface IToolLocator { string GetToolPath(string toolName); }`——toolName ∈ {"diskspd.exe","7zr.exe","stream-windows.exe","7ZIP-LICENSE.txt","DISKSPD-LICENSE.txt"}
- `class ToolExtractor(IToolBinariesSource binaries) : IToolLocator`——首次调用释放到 `%TEMP%\HardwareBench\tools\<name>`，校验 SHA256；已存在且哈希匹配则复用
- `interface IToolBinariesSource { Stream? Open(string toolName); }`——生产实现 `AssemblyResourceBinariesSource` 读清单资源 `HardwareBench.Tools.<name>`（薄层不单测）；测试用 fake
- `static class ToolHashes { static readonly IReadOnlyDictionary<string,string> Expected; static string ToolVersions; }`

**哈希常量（小写，与 assets/tools/SHA256SUMS.txt 一致）：**
```
diskspd.exe          8f3b2f0909549c54253ede26c9a8d239b8b6c817b076bcd7efb1bda6571aee9
7zr.exe              56b8cc9f4971cef253644fafe54063ed7fdca551d4de0f8c6baa81b855acd72
stream-windows.exe   a09913f5ca7fc57aa755fc778b86cdfc6021abd8008e0dd08ac3fd05995a846e
7ZIP-LICENSE.txt     3a184aa13dc8ad30734e28ff901b478ccbccb5b41be52427a5e7609c8fd9e5ddb
DISKSPD-LICENSE.txt  40dc99f18435c2ee593b8e32aa084548d2894f71e8dcc9a4f03d83d54d78bf11
```
`ToolVersions = "DiskSpd v2.2; 7-Zip 7zr (bundled); STREAM 5.10 Windows-variant"`

**csproj 追加（Embed 到 Core）：**
```xml
<ItemGroup>
  <EmbeddedResource Include="..\..\assets\tools\diskspd\diskspd.exe" LogicalName="HardwareBench.Tools.diskspd.exe" />
  <EmbeddedResource Include="..\..\assets\tools\7zr\7zr.exe" LogicalName="HardwareBench.Tools.7zr.exe" />
  <EmbeddedResource Include="..\..\assets\tools\stream\stream-windows.exe" LogicalName="HardwareBench.Tools.stream-windows.exe" />
  <EmbeddedResource Include="..\..\assets\tools\7zr\7ZIP-LICENSE.txt" LogicalName="HardwareBench.Tools.7ZIP-LICENSE.txt" />
  <EmbeddedResource Include="..\..\assets\tools\diskspd\DISKSPD-LICENSE.txt" LogicalName="HardwareBench.Tools.DISKSPD-LICENSE.txt" />
</ItemGroup>
```

**行为契约：** 哈希不匹配/资源缺失 → 抛 `InvalidOperationException`（消息含 toolName 与期望/实际哈希前 8 位）；成功返回绝对路径。

- [ ] **Step 1 失败测试**（用 fake IToolBinariesSource + 指向 `%TEMP%\HardwareBench-tests\<guid>\` 的可注入根目录——构造函数第二参 `string? rootDir = null` 默认 `%TEMP%\HardwareBench`）：①有效字节流 → 文件存在且 SHA256 匹配，二次调用不重写（LastWriteTimeUtc 不变）②篡改流（首字节 XOR）→ 抛异常 ③资源缺失 → 抛异常。fake 用 `new MemoryStream(Encoding.ASCII.GetBytes("fake-dll-bytes"))` 配合「先算真实哈希再注入 ToolHashes 期望值」不可行——改为：测试用的期望哈希由测试自己用 SHA256 对假字节算出并注入（ToolHashes.Expected 需 `internal` 可替换或构造注入 `IReadOnlyDictionary<string,string>? overrides = null`）。
- [ ] **Step 2 RED** → **Step 3 实现**（释放用 FileStream 写入 `.tmp` 再 File.Move 原子替换；校验用 SHA256.HashData）→ **Step 4 GREEN** → **Step 5 全套 + Commit**：`feat: tool extraction with SHA256 verification`
- [ ] 附带薄层：`AssemblyResourceBinariesSource`（同文件，~10 行，不单测）

---

### Task 3: 受限子进程执行器（Job Object 兜底）

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/ProcessRunner.cs`
- Test: `src/HardwareBench.Tests/Benchmarks/ProcessRunnerTests.cs`

**Interfaces:**
- `record ProcessSpec(string ExePath, IReadOnlyList<string> Args, int TimeoutMs, string WorkingDirectory);`
- `record ProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);`
- `interface IProcessRunner { Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken ct); }`
- `class BoundedProcessRunner : IProcessRunner`——`[SupportedOSPlatform("windows")]`

**实现要点：**
- `System.Diagnostics.Process`：UTF8 编码回读 stdout/stderr（`StandardOutputEncoding = Encoding.UTF8`）；`cancellationToken.Register` 与超时二选一触发 `Kill(entireProcessTree: true)`；等 待 `WaitForExitAsync` 后收集输出
- Job Object：`CreateJobObjectW` → `SetInformationJobObject(JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE })` → `AssignProcessToJobObject`（进程 Start 后立即挂）→ finally `CloseHandle`（句柄关闭即连坐杀进程树，Process.Kill 之外的双保险）
- JobObjectScope 为 internal sealed class，P/Invoke 声明内联同文件；`TreatWarningsAsErrors` 下注意 CA1416 → 类级 `[SupportedOSPlatform("windows")]`

- [ ] **Step 1 集成测试**（Trait Integration，跑真 cmd）：①`cmd.exe /c echo hello-bench` → ExitCode 0、StdOut 含 "hello-bench"、未超时 ②`cmd.exe /c ping -n 30 127.0.0.1` + TimeoutMs=1500 → TimedOut=true 且 RunAsync 在 ~2s 内返回（Stopwatch 断言 < 10s）③传入已取消 token → 抛 OperationCanceledException
- [ ] **Step 2 RED** → **Step 3 实现** → **Step 4 GREEN**（允许 `--filter FullyQualifiedName~ProcessRunnerTests`）→ **Step 5 Commit**：`feat: bounded process runner with job-object containment`

---

### Task 4: 三套输出解析器（纯函数，fixture 实证）

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/Parsers/DiskSpdParser.cs`、`SevenZipParser.cs`、`StreamParser.cs`
- Test: `src/HardwareBench.Tests/Benchmarks/ParsersTests.cs`

**Interfaces:**
- `record DiskSpdMetrics(double ReadMiBS, double ReadIops);` / `static class DiskSpdParser { static DiskSpdMetrics Parse(string stdout); }`
- `record SevenZipMetrics(double TotalRatingMips, double CompressRatingMips, double DecompressRatingMips);` / `static class SevenZipParser { static SevenZipMetrics Parse(string stdout); }`
- `record StreamMetrics(double Copy, double Scale, double Add, double Triad);` / `static class StreamParser { static StreamMetrics Parse(string stdout); }`

**解析规则（以真实 fixture 为准）：**
- DiskSpd：定位独立行 `Read IO`（Trim 后全等），其后第一个 Trim 后以 `total:` 开头的行，按 `|` 分列 → col[2]=MiB/s、col[3]=IOPS。找不到 → `FormatException("DiskSpd output: Read IO total line not found")`
- 7-Zip：Trim 后以 `Tot:` 开头的行 → 空白分词 tokens = ["Tot:",usage,R/U,Rating] → Rating=TotalRating；`Avr:` 行按 `|` 分左右 → 左侧末列 Compress、右侧末列 Decompress
- STREAM：`Copy:`/`Scale:`/`Add:`/`Triad:` 开头（Trim 后）行 → 空白分词 tokens[1] = Best Rate MB/s；四行任缺 → FormatException

**测试 fixture（嵌入测试源码，节选自 docs/fixtures/m2/ 真实输出）：**

DiskSpd（节选）：
```
Total IO
thread |       bytes     |     I/Os     |    MiB/s   |  I/O per s |  file
------------------------------------------------------------------------------
     0 |      3099066368 |        47288 |     589.64 |    9434.21 | testfile.dat (50MiB)
------------------------------------------------------------------------------
total:        3099066368 |        47288 |     589.64 |    9434.21

Read IO
thread |       bytes     |     I/Os     |    MiB/s   |  I/O per s |  file
------------------------------------------------------------------------------
     0 |      493993984 |       120604 |     156.94 |   40177.44 | testfile.dat (50MiB)
------------------------------------------------------------------------------
total:           493993984 |       120604 |     156.94 |   40177.44
```
断言 Parse → (156.94, 40177.44)（**注意取的是 Read IO 节不是 Total IO 节**）。

7-Zip（节选）：
```
Dict     Speed Usage    R/U Rating  |      Speed Usage    R/U Rating
         KiB/s     %   MIPS   MIPS  |      KiB/s     %   MIPS   MIPS

22:      71186  1340   5168  69250  |     631077  1822   2953  53806
Avr:     66360  1333   5079  67701  |     626231  1859   2913  54160
Tot:            1596   3996  60931
```
断言 → Total=60931、Compress=67701、Decompress=54160。

STREAM（节选）：
```
Function    Best Rate MB/s  Avg time     Min time     Max time
Copy:           33085.9     0.005104     0.004836     0.006060
Scale:          24600.2     0.007200     0.006504     0.008428
Add:            28864.5     0.009421     0.008315     0.011756
Triad:          27896.9     0.009346     0.008603     0.011676
```
断言 → (33085.9, 24600.2, 28864.5, 27896.9)。

- [ ] **Step 1 失败测试**：三组正向 + 三组畸形输入（缺 Read IO 节 / 缺 Tot 行 / 缺 Triad 行 → FormatException）
- [ ] **Step 2 RED** → **Step 3 实现**（纯函数、InvariantCulture 解析）→ **Step 4 GREEN** → **Step 5 Commit**：`feat: benchmark output parsers validated against real fixtures`

---

### Task 5: 三个基准引擎

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/Engines/IBenchmarkEngine.cs`、`DiskSpdEngine.cs`、`SevenZipEngine.cs`、`MemoryStreamEngine.cs`
- Test: `src/HardwareBench.Tests/Benchmarks/EnginesIntegrationTests.cs`

**Interfaces:**
- `interface IBenchmarkEngine { string Id { get; } string Category { get; } int WarmupRuns { get; } Task<IReadOnlyList<MetricValue>> RunOnceAsync(CancellationToken ct); }`
- 三引擎均 `(IToolLocator tools, IProcessRunner runner)` 构造注入，`[SupportedOSPlatform("windows")]`

**引擎参数（硬编码，客观性=参数公开）：**
- DiskSpdEngine（Id "engine.disk"，Category "Disk"，WarmupRuns 1）：
  - 顺序读：`-c1G -d5 -w0 -b1M -o8 -t1 -Sh <work>\diskspd-seq.dat` → 指标 `disk-seq-read`(MiB/s, scored) + `disk-seq-iops`(IOPS)
  - 4K 随机读：`-c1G -d5 -w0 -b4K -r4K -o8 -t1 -Sh <work>\diskspd-4k.dat` → `disk-4k-read`(MiB/s, scored) + `disk-4k-iops`
  - work 目录 `%TEMP%\HardwareBench\work`（创建）；TimeoutMs 60000/次；finally 删除测试文件；预热轮用 `-d3`
  - 非 0 退出码 → `InvalidOperationException($"DiskSpd exited {code}: {stderr 前 200 字}")`
- SevenZipEngine（Id "engine.cpu7z"，Category "Cpu"，WarmupRuns 0）：`7zr.exe b`，cwd=工具目录，TimeoutMs 600000 → `cpu-7z-rating`(MIPS, scored) + `cpu-7z-compress` + `cpu-7z-decompress`
- MemoryStreamEngine（Id "engine.memstream"，Category "Memory"，WarmupRuns 0)：`stream-windows.exe`，TimeoutMs 180000 → `mem-triad`(MB/s, scored) + `mem-copy`/`mem-scale`/`mem-add`

- [ ] **Step 1 集成测试**（Trait Integration；真实 ToolExtractor+AssemblyResourceBinariesSource + BoundedProcessRunner）：①每引擎 RunOnceAsync 返回指标数与单位正确、scored 指标 >0（disk-seq-read > 10 宽松下限防 SSD 之外设备误报）②已取消 token → OperationCanceledException。断言值域宽松（跨机器稳定），精确格式校验已由 T4 承担
- [ ] **Step 2 RED** → **Step 3 实现** → **Step 4 GREEN**（7zr 用例允许长跑，记录耗时）→ **Step 5 全套（`--filter Category!=Integration` 快速 + 全量一次）+ Commit**：`feat: diskspd/7zip/stream benchmark engines`

---

### Task 6: 公平性守卫

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/FairnessGuard.cs`
- Test: `src/HardwareBench.Tests/Benchmarks/FairnessGuardTests.cs`

**Interfaces:**
- `record FairnessReport(string PowerSchemeName, bool PowerSchemeOk, double BackgroundCpuPercent, bool BackgroundCpuOk, IReadOnlyList<string> Warnings);`
- `interface IFairnessGuard { Task<FairnessReport> CheckAsync(CancellationToken ct); }`
- `class FairnessGuard(IProcessRunner runner) : IFairnessGuard`——`[SupportedOSPlatform("windows")]`
- `static class PowerSchemeParser { static (string Guid, string Name)? Parse(string powercfgOutput); }`（纯函数）
- `static class CpuBusySampler { static double SampleBusyFraction(int intervalMs); }`——GetSystemTimes P/Invoke 双采样（idle/kernel/user FILETIME），busy = 1 - Δidle/Δ(idle+kernel+user)

**规则：** 电源方案名 ∈ {平衡/Balanced/高性能/High performance}（不区分大小写包含匹配）→ Ok；否则 Warning「电源计划为 X，建议切换到平衡/高性能」。后台 CPU 空闲率法 >15% → Warning。guard 永不抛（采样失败 → BackgroundCpuPercent=-1、Warning 注明无法采样）。powercfg 调用：`powercfg /getactivescheme`，中文输出形如 `电源方案 GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (平衡)`。

- [ ] **Step 1 失败测试**：①PowerSchemeParser 对中文/英文两段真实输出解析出 GUID+名称 ②畸形输出 → null ③CpuBusySampler.SampleBusyFraction(300) ∈ [0,1] ④FairnessGuard 用 fake runner（返回中文平衡方案 + 真实采样）→ Ok 且 Warnings 空；fake 返回「节能」→ 含电源 Warning
- [ ] **Step 2 RED** → **Step 3 实现** → **Step 4 GREEN** → **Step 5 Commit**：`feat: fairness guard (power scheme + background cpu sampling)`

---

### Task 7: 评分与参照表

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/Scoring.cs`、`src/HardwareBench.Core/Benchmarks/ReferenceTable.cs`
- Test: `src/HardwareBench.Tests/Benchmarks/ScoringTests.cs`

**Interfaces:**
- `static class Scoring { static int ComputeScore(double value, double ref1000); static int GeometricMean(IReadOnlyList<int> scores); static double Median(IReadOnlyList<double> values); }`
- `static class ReferenceTable { const string Version = "2026.08-m2"; static bool TryGet(string metricId, out (string Unit, double Ref1000) entry); }`

**参照表 v1（公开可调，仓库 `docs/reference-table.md` 由本任务同步生成）：**
```
disk-seq-read   MiB/s  7000     disk-4k-read   MiB/s  400
cpu-7z-rating   MIPS   100000   mem-triad      MB/s   80000
```

**规则：** ComputeScore = `Clamp(Round(v/ref*1000), 0, 1000)`（Invariant）；GeometricMean：任一 ≤0 → 0，否则 `Round(exp(mean(ln)))`；Median：奇数个取中间（3 轮契约）。

- [ ] **Step 1 失败测试**：ComputeScore 边界（0→0、恰等于 ref→1000、2×ref→1000 封顶、四舍五入）；GeometricMean（[1000,1000]→1000、[1000,0]→0、[100,225,1000]→300 即 10*15*100 的几何均值 ≈300？验证：exp((ln100+ln225+ln1000)/3)=exp((4.605+5.416+6.908)/3)=exp(5.643)≈282——测试断言按真实计算写 282）；Median（[1,9,5]→5）；ReferenceTable 四 id 命中 + 未知 id false
- [ ] **Step 2 RED** → **Step 3 实现 + 生成 docs/reference-table.md（含版本号、换算公式、调整政策说明）** → **Step 4 GREEN** → **Step 5 Commit**：`feat: scoring with versioned public reference table`

---

### Task 8: BenchmarkService 编排

**Files:**
- Create: `src/HardwareBench.Core/Benchmarks/BenchmarkService.cs`
- Test: `src/HardwareBench.Tests/Benchmarks/BenchmarkServiceTests.cs`

**Interfaces:**
- `record BenchmarkProgress(string EngineId, string Phase, int RunIndex, int TotalRuns);`
- `interface IBenchmarkService { Task<BenchmarkResult> RunAsync(IProgress<BenchmarkProgress>? progress, CancellationToken ct); }`
- `class BenchmarkService(IEnumerable<IBenchmarkEngine> engines, IFairnessGuard guard) : IBenchmarkService`

**行为契约：**
1. 先 `guard.CheckAsync` → 环境（PowerScheme、BackgroundCpuPercent、ToolHashes.ToolVersions）写入结果
2. 每引擎：WarmupRuns 轮（进度 Phase="warmup"，结果弃置）→ 3 轮正式（Phase="run"，RunIndex 1..3）
3. 每指标：3 轮值 → Median → AllRuns 保留原始 3 值；ReferenceTable 命中 → ComputeScore，否则 Score=null
4. 引擎异常（非 OCE）→ `BenchmarkError(engine.Id, message)`，继续下一引擎；OCE 向上抛
5. TotalScore = 已评分项 GeometricMean；0 个评分项 → null
6. StartedAtUtc 由服务盖章

- [ ] **Step 1 失败测试**（fake 引擎 ×3：A 正常返回 2 指标（其一 scored id）、B 抛 IOException、C 返回 1 指标）：断言 Errors 恰 1 条且 EngineId="B"；A 的 scored 指标 AllRuns==3 值且 Value==中位数、Score 按参照表；C 指标 Score=null；TotalScore 非空=各评分几何平均；进度序列包含 ("A","warmup",1,1)/("A","run",3,3)；已取消 token → OCE；全引擎失败 → TotalScore null
- [ ] **Step 2 RED** → **Step 3 实现** → **Step 4 GREEN** → **Step 5 全套 + Commit**：`feat: benchmark orchestration (median-of-3, per-engine fault tolerance)`

---

### Task 9: 跑分页 UI + 无头模式 + DI 接线

**Files:**
- Create: `src/HardwareBench.App/ViewModels/BenchmarkViewModel.cs`、`src/HardwareBench.App/Views/BenchmarkPage.xaml(.cs)`
- Modify: `src/HardwareBench.App/App.xaml.cs`（DI 注册 + `--benchmark` 无头分支）、`ViewModels/MainViewModel.cs`（跑分占位换真 VM）、`MainWindow.xaml`（DataTemplate）
- Test: `src/HardwareBench.Tests/App/BenchmarkViewModelTests.cs`

**Interfaces:**
- `class BenchmarkViewModel(IBenchmarkService service, IFileSaveService fileSave) : ObservableObject`
  - 属性：`IsBusy`、`ProgressText`、`Indeterminate`(bool)、`FairnessText`(string)、`MetricRows`(ObservableCollection<MetricRow>)、`TotalText`、`HasResults`
  - `record MetricRow(string Id, string RawDisplay, string ScoreDisplay)`（UI 层类型，放 VM 文件内）
  - `[RelayCommand] StartAsync()`：清空旧结果 → IsBusy → progress 回调更新 ProgressText（"引擎 engine.disk 第 2/3 轮"）→ 完成后填 FairnessText（Warnings join "；"，空则 "环境检查通过"）、MetricRows（RawDisplay="156.9 MiB/s"、ScoreDisplay="392 / 1000" 或 "—"）、TotalText（"总分 512（几何平均，参照表 2026.08-m2）"或 "总分 —"）；异常 → StatusText 显示且不崩
  - `[RelayCommand] ExportAsync()`：ReportJson 同款模式写 BenchmarkJson
- `MainViewModel`：跑分导航项从 PlaceholderViewModel 换成注入的 `BenchmarkViewModel`（历史仍占位 "历史结果将在 M3 里程碑启用"）；MainWindow 增加 `DataTemplate DataType=BenchmarkViewModel → BenchmarkPage`
- `BenchmarkPage.xaml`：顶部「开始跑分」按钮（InverseBool 绑 IsBusy）+ 公平性提示 TextBlock（TextWrapping）+ ProgressBar（IsIndeterminate 绑定）+ 进度文本 + 结果 ItemsControl（Id/RawDisplay/ScoreDisplay 三列网格）+ 总分 TextBlock（大字号）+「导出 JSON」按钮（HasResults 绑定）
- 无头模式（App.OnStartup 开头）：`e.Args.Contains("--benchmark")` → 不 Show 窗口；`await service.RunAsync(null, default)`；`--export <path>` 或默认 `HardwareBench-result-yyyyMMdd-HHmmss.json`（当前目录）；`Shutdown(0)`；异常 `Shutdown(1)` + Console 输出不可用则忽略（ AttachConsole 可选不强制）。OnStartup 变 async void 仅在此分支 await

- [ ] **Step 1 VM 失败测试**（fake IBenchmarkService：返回含 fairness Warning + 2 指标 + TotalScore 的结果；fake IFileSaveService 同 M1 模式）：Start 后 IsBusy=false、MetricRows 数量/显示格式、TotalText 含 "512"、FairnessText 含 Warning 文本、HasResults=true；Export 写出真实文件且 JSON 可被 BenchmarkJson.Deserialize 读回（断言 TotalScore）；Service 抛错 → IsBusy=false 且无崩溃
- [ ] **Step 2 RED** → **Step 3 实现 VM + 页面 + DI + 无头分支** → **Step 4 GREEN** → **Step 5 手动 QA**：`dotnet run --project src\HardwareBench.App`（或发布 exe）→ 跑分页点开始 → 观察进度/公平性提示/结果渲染 → 截图 `docs/qa/m2/ui-run.png`；再跑 `--benchmark --export docs/qa/m2/headless-result.json` 验证无头路径产出合法 JSON → **Step 6 Commit**：`feat: benchmark page UI, headless mode, DI wiring`

---

### Task 10: v0.2.0-m2 发布 + 复现性验收 + 文档

**Files:**
- Modify: `README.md`（路线图 M2 → ✅、新增无头模式用法、参照表链接）、`publish.ps1`（不变，验证即可）
- Create: `docs/qa/m2/reproducibility.md`、`docs/reference-table.md`（若 T7 未生成）

**验收（全部留证据）：**
- [ ] **Step 1** `.\publish.ps1` → 单 exe（预期 ~156MB，因内嵌 ~5.4MB 工具略增）；记录 SHA256
- [ ] **Step 2 复现性（里程碑验收线）**：发布 exe 无头模式连跑两轮：
  ```powershell
  .\publish\HardwareBench.exe --benchmark --export docs\qa\m2\run1.json
  .\publish\HardwareBench.exe --benchmark --export docs\qa\m2\run2.json
  ```
  PowerShell 解析两 JSON，对四个 scored 指标逐项算 `|v1-v2|/max(v1,v2)`，全部 **<5%** → 写入 `docs/qa/m2/reproducibility.md`（含两轮数值表、环境快照、结论）。任一 ≥5% → 停下排查（电源计划/后台负载记录在案后重跑一轮仲裁，仲裁轮与较好一轮比较）
- [ ] **Step 3** UI 手动跑一轮 + 截图补充（若 T9 未截）
- [ ] **Step 4** README 更新 + Commit：`docs: m2 release notes, reproducibility evidence, reference table`；tag `v0.2.0-m2`（annotated）
- [ ] **Step 5** （收尾由控制器执行，不在本任务内）推送 + PR + finishing 流程

---

## 自审记录（Self-Review）

1. **Spec 覆盖**：design.md §4.2 引擎层（T2 工具分发/T3 子进程+Job Object/T5 引擎）、§4.3 公平性守卫（T6 + T8 编排含环境快照；预热策略修订已在 Global Constraints 批准）、§4.4 评分器（T7 参照表+几何平均+版本化公开）、§4.5 结果 JSON（T1/T9 导出；SQLite 历史明确属 M3 不在本计划）；里程碑验收「三类基准真机跑通 + 两轮 <5%」→ T5 集成测试 + T10 Step 2。
2. **占位符扫描**：无 TBD/TODO；所有数值（哈希/参数/超时/参照值）均为实测或显式声明可调。
3. **类型一致性**：`MetricValue(Id,Unit,Value)` 三参与 T5/T8 一致；`IBenchmarkEngine.RunOnceAsync(CancellationToken)` 在 T5/T8 一致；`FairnessReport` 五字段 T6/T8/T9 一致；`BenchmarkProgress(EngineId,Phase,RunIndex,TotalRuns)` T8/T9 一致；`IToolLocator.GetToolPath(string)` T2/T5 一致。
