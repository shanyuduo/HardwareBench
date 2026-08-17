# 硬件检测与测评程序 · 设计文档

- 日期：2026-08-17
- 状态：已获老板逐节确认
- 轮子调研报告：`D:\OpenCodePortable\WORKSPACE\2026-08-17-硬件检测跑分轮子调研\硬件检测与跑分轮子调研报告.md`

## 1. 背景与目标

现有硬件检测与测评软件（鲁大师等商业程序）臃肿、广告多、分数不客观。本项目做一个**轻量、客观、无广告**的 Windows 硬件检测与测评程序，最大化复用成熟开源轮子（详见调研报告），仅自研轮子覆盖不到的部分。

**一句话定位**：检测 + 跑分一体的原生 GUI 便携工具，分数可复现、方法论全公开。

## 2. 需求快照（已确认）

| 维度 | 决定 |
|---|---|
| MVP 范围 | 硬件全量检测（CPU/主板/内存/显卡/硬盘/屏幕/外设）+ 四大件跑分（CPU/GPU/内存/磁盘） |
| 形态 | 原生 GUI（WPF, MVVM） |
| 技术栈 | C# / .NET 8 |
| 分数哲学 | 真实物理值 + 公开公式换算分 + 预留数据库比对 |
| 发布 | 便携单文件 exe（self-contained），GitHub + Gitee 双推送 |
| 语言 | 中文优先，资源字典预留 i18n |
| 本项目许可 | MIT（发布携带 THIRDPARTY-NOTICES.md） |

**非目标（v1 不做）**：压力测试/烤机、驱动管理、系统清理、广告与推广、云数据库强制上传。

## 3. 整体架构

.NET 8 WPF 单体应用（MVVM），三层：

```
┌─ UI 层（Views + ViewModels）─────────────────────┐
│  硬件检测页 │ 跑分页 │ 历史结果页                  │
├─ 服务层 ─────────────────────────────────────────┤
│  DetectionService   聚合各来源 → HardwareReport   │
│  BenchmarkService   队列调度 IBenchmarkEngine     │
│  ScoringService     物理值→分项分→几何平均总分     │
├─ 基础设施层 ──────────────────────────────────────┤
│  Hardware.Info(WMI) │ EDID解析器(自写) │ SMART(IOCTL) │
│  LHM传感器(可选,管理员) │ 基准引擎wrappers(子进程)  │
└───────────────────────────────────────────────────┘
```

## 4. 组件设计

### 4.1 检测器（DetectionService）

| 来源 | 轮子/方式 | 覆盖内容 | 备注 |
|---|---|---|---|
| 系统清单 | Hardware.Info（NuGet, MIT） | CPU/主板/内存条(容量/速度/厂商/序列号)/显卡/存储/网卡/OS | 纯 WMI，无权限问题 |
| 屏幕 | 自写 EDID 解析器 | 型号/分辨率/刷新率/面板厂商/色深 | SetupAPI + 注册表 EDID blob；Windows 无成熟库，自写 |
| 磁盘健康 | P/Invoke IOCTL storage query | SMART 属性/温度/通电时间/健康度 | 参照 CrystalDiskInfo（MIT）实现 |
| 外设 | SetupAPI 枚举 USB/HID | 键鼠/打印机等设备列表 | |
| 传感器 | LibreHardwareMonitorLib（NuGet, MPL-2.0） | 温度/风扇/电压 | **默认关闭**；开启需管理员并显示风险提示（WinRing0/Defender 争议，见调研报告 §4.3） |

输出统一聚合为 `HardwareReport` 模型（JSON 可序列化），UI 直接绑定。

### 4.2 基准引擎层（BenchmarkService）

统一接口：

```csharp
interface IBenchmarkEngine
{
    string Id { get; }            // "disk-seq", "cpu-lzma", ...
    string Category { get; }      // Disk / Cpu / Memory / Gpu
    Task PrepareAsync();          // 释放内嵌工具、校验环境
    Task<BenchmarkResult> RunAsync(IProgress<BenchmarkProgress> progress, CancellationToken ct);
    BenchmarkResult Parse(string stdout);  // 纯函数，可单测
}
```

| 引擎 | 工具 | 许可证 | 分发方式 | 指标 |
|---|---|---|---|---|
| 磁盘顺序/4K | Microsoft DiskSpd | MIT | 内嵌资源→%TEMP% | MB/s、IOPS、延迟 |
| CPU 整数/压缩 | 7zr.exe `7zr b` | LGPL | 内嵌资源 | MIPS |
| CPU 多算法 | openssl speed | Apache-2.0 | 内嵌资源 | 多算法 ops/s |
| 内存带宽/延迟 | STREAM（自行编译） | 宽松自定义 | 内嵌资源 | GB/s |
| GPU 渲染 | Blender headless + 官方场景 | GPL/CC | **不内嵌**，检测系统安装，未装引导 | samples/min |

- 子进程用 **Windows Job Object** 兜底，确保任何路径下都能干净终止。
- stdout 解析器是纯函数，用录制的真实输出做单元测试 fixture。

### 4.3 公平性守卫（客观性的关键）

跑分启动前自动检查：
1. 电源计划非「高性能/平衡」→ 提示切换（不强制）；
2. 后台 CPU 占用 > 15% → 警告并建议关闭后台程序；
3. 每项基准：预热 1 轮 + 正式 3 轮，**取中位数**；
4. 结果 JSON 记录环境快照（电源计划/后台负载/驱动版本/引擎版本）。

### 4.4 评分器（ScoringService）

- **原始值优先展示**：MB/s、GB/s、MIPS、samples/min——分数永远附在物理值旁。
- **分项分**：查公开参照表线性换算 0–1000 分。参照表内置于应用，同时在仓库以 markdown 公开（版本化）。
- **总分**：分项分**几何平均**（Phoronix Test Suite 同款方法，避免单项霸权）。UI 明示「总分仅在同版本参照表内有意义」。
- **可复现**：所有工作负载参数硬编码并公开；结果 JSON 含完整环境快照。
- 结果 schema 预留 `upload` 字段（数据库比对 v2 用）。

### 4.5 结果存储

- 本地历史：SQLite（单文件，放 exe 同目录 `data/` 下，保持便携）。
- 导出：JSON（完整报告 + 分数 + 环境快照）。

### 4.6 UI（WPF, MVVM）

- 三个主页面：硬件检测 / 跑分 / 历史结果。
- 中文优先，字符串入资源字典预留 i18n。
- 现代 flat 暗色主题；跑分页有进度条 + 实时日志。

## 5. 数据流

- **检测**：WMI/EDID/SMART/SetupAPI → DetectionService 聚合 → `HardwareReport` → UI 绑定。
- **跑分**：用户点击开始 → 公平性检查 → 引擎队列逐项运行（进度+日志）→ 原始物理值 → ScoringService → 分项分 + 总分 → SQLite 历史 + JSON 导出。

## 6. 错误处理

| 场景 | 策略 |
|---|---|
| 子进程崩溃/超时 | 重试 1 次 → 仍失败该项 N/A，不阻塞其他项；总分按剩余项几何平均并在结果注明 |
| WMI 单项失败 | 字段显示「未知」，不弹窗 |
| 传感器权限不足 | 提示需管理员，其余功能零影响 |
| 杀软误报（释放 exe） | 仓库提供 SHA256 清单 + 说明页；远期代码签名 |
| 跑分中强制关闭 | Job Object 干净终止所有子进程 |

## 7. 测试策略（TDD）

- **单元测试**：EDID 解析（真实显示器 hex 样本）、评分换算（参照表边界值）、各引擎 stdout 解析器（录制 fixture）。
- **集成测试**：每个基准引擎真跑 smoke（轻量参数）。
- **UI**：手动 QA + 截图走查；如需自动化用 FlaUI。
- 任何 bug 修复先写回归测试。

## 8. 许可证合规

- 本项目 **MIT**。
- NuGet：Hardware.Info（MIT，无义务）；LibreHardwareMonitorLib（MPL-2.0，文件级隔离不传染，保留版权声明）。
- 内嵌分发：DiskSpd（MIT）、7zr.exe（LGPL，不用含 unRAR 限制的 7z.exe 完整版）、STREAM（宽松自定义，附声明）。
- Blender 不内嵌，进程边界调用，GPL 不传染。
- 发布物携带 `THIRDPARTY-NOTICES.md`。

## 9. 里程碑

| 里程碑 | 内容 | 验收标准 |
|---|---|---|
| M1 | 检测完成（WMI+EDID+SMART+外设，无传感器） | 三页面 UI 可用，检测报告正确导出 JSON |
| M2 | 磁盘+CPU+内存跑分闭环 + 评分 | 三类基准真机跑通，分数可复现（同机两轮偏差 <5%） |
| M3 | GPU 跑分（Blender）+ 历史结果 + 传感器可选模式 | 全功能可用，便携 exe 发布 |

## 10. 风险与对策

| 风险 | 对策 |
|---|---|
| WinRing0 被 Defender 拦截 | 传感器默认关闭、可选管理员模式 + 明确风险提示 |
| 内嵌释放 exe 触发杀软 | SHA256 清单 + 说明页；远期代码签名 |
| 参照表被质疑偏向 | 表版本化公开，接受社区 PR 校准 |
| Blender 未安装 | 检测引导，GPU 分项可跳过 |

## 11. 已决事项

- 方案 A：WPF 外壳 + NuGet 引用检测库 + 子进程基准引擎（老板已确认）。
- 不 fork LHM；不默认加载 ring0 驱动。
- v1 不做云数据库，schema 预留。
