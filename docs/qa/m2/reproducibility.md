# M2 复现性验收报告（v0.2.0-m2）

- 日期：2026-08-18
- 被测对象：`publish\HardwareBench.exe`（v0.2.0-m2 单文件发布版）
- 验收线：四个 scored 指标两轮运行相对偏差 `|v1-v2| / max(v1,v2)` 全部 **< 5%**
- 结论：**PASS**

## 发布产物

| 项 | 值 |
|---|---|
| 文件 | `publish\HardwareBench.exe` |
| 大小 | 163,577,989 字节（156.0 MB） |
| SHA256 | `7411DECFAC30245A4AFDDA27B175049A92D3AE9740B6B20E466670095441BD9D` |
| 构建 | `.\publish.ps1`（Release / win-x64 / self-contained single-file） |

## 运行方式

无头模式连跑两轮（每轮约 3.3 分钟）：

```powershell
.\publish\HardwareBench.exe --benchmark --export docs\qa\m2\run1.json
.\publish\HardwareBench.exe --benchmark --export docs\qa\m2\run2.json
```

- run1：退出码 0（JSON 完整写出、无 `.error.txt`，headless 仅在成功后写 JSON 并 `Shutdown(0)`），耗时 3.23 分钟
- run2：退出码 0（`Start-Process -Wait -PassThru` 捕获），耗时 3.28 分钟
- 两轮 `Errors` 均为空

## 两轮数值表

| 指标 ID | 单位 | run1 | run2 | 相对偏差 \|v1-v2\|/max |
|---|---|---|---|---|
| `disk-seq-read` | MiB/s | 3661.48 | 3576.36 | **2.32%** |
| `disk-4k-read` | MiB/s | 310.70 | 317.22 | **2.06%** |
| `cpu-7z-rating` | MIPS | 55055 | 53073 | **3.60%** |
| `mem-triad` | MB/s | 19850.3 | 20250.4 | **1.98%** |
| 总分（几何均值） | — | 485 | 483 | 0.41% |

四个 scored 指标全部 < 5%，**通过复现性验收线**，无需仲裁轮。

## 环境快照

| 项 | run1 | run2 |
|---|---|---|
| 电源计划 | 平衡 | 平衡 |
| 后台 CPU 占用 | 50.7% | 51.1% |
| 公平性警告 | 后台 CPU 占用 51%，建议关闭占用程序后重测 | 后台 CPU 占用 51%，建议关闭占用程序后重测 |
| 工具版本 | DiskSpd v2.2; 7-Zip 7zr (bundled); STREAM 5.10 Windows-variant | 同左 |

> 注：本开发机存在约 50% 的持续后台 CPU 占用（任务管理器可见），两轮均触发公平性警告。即便如此，四个指标偏差仍全部 < 5%，说明 median-of-3 聚合与各引擎内部多轮取中位数有效抑制了噪声。

## 机器上下文

- 开发机：Windows（win-x64），NVMe 系统盘
- 参照表：`docs/reference-table.md`（版本 `2026.08-m2`）
- 测试时间：2026-08-18 02:54–03:01（本地，UTC+8）

## 结论

**PASS** — v0.2.0-m2 发布版在无头模式下两轮完整跑分复现性达标（全部 < 5%），可进入发布流程。