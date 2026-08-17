# 基准评分参照表

版本：`2026.08-m2`

本表定义各基准指标的 1000 分锚点（Ref1000）。分数由原始测量值按公式换算，用于跨机器横向比较。

## 参照表

| 指标 ID | 单位 | Ref1000 锚点 | 对应引擎 |
|---------|------|--------------|----------|
| `disk-seq-read` | MiB/s | 7000 | DiskSpd 顺序读 |
| `disk-4k-read` | MiB/s | 400 | DiskSpd 4K 随机读 |
| `cpu-7z-rating` | MIPS | 100000 | 7-Zip 基准（Tot Rating） |
| `mem-triad` | MB/s | 80000 | STREAM Triad |

## 换算公式

单项分数：

```
分数 = clamp(round(v / Ref × 1000), 0, 1000)
```

- `v`：原始测量值；`Ref`：上表锚点。
- 四舍五入采用 AwayFromZero（0.5 向上取整）。
- 结果截断到 [0, 1000]；NaN 或负值按 0 计。

总分（几何均值）：

```
总分 = round(exp(mean(ln(score₁), ln(score₂), …)))
```

- 任一单项分数 ≤ 0 时总分为 0。

## 调整政策

- 锚点基于 2026 年 8 月真实运行校准：开发机 NVMe 上 DiskSpd 顺序读约 2950 MiB/s、7-Zip Tot 约 60931 MIPS、STREAM Triad 约 27897 MB/s。
- 参照表为公开可调：如需调整锚点，通过 PR 修改 `ReferenceTable.cs` 并同步更新本文档，同时递增版本号（如 `2026.09-m2`）。