# HardwareBench

**轻量、客观、无广告的 Windows 硬件检测与测评程序**

English name: **HardwareBench** — a lightweight, objective, ad-free hardware detection and benchmarking tool for Windows.

![startup screenshot](docs/qa/m1/startup.png)

## 构建方法

```powershell
.\publish.ps1
```

输出为 `publish\HardwareBench.exe`（单文件自包含，无需 .NET 运行时）。

## 路线图

| 里程碑 | 内容 | 状态 |
|---|---|---|
| M1 | 硬件检测（CPU/主板/内存/显卡/硬盘/屏幕/外设） | ✅ 已交付 |
| M2 | 磁盘+CPU+内存跑分闭环 + 评分 | 🚧 进行中 |
| M3 | GPU 跑分 + 历史结果 + 传感器可选模式 | 📋 规划中 |

## 客观性承诺

- 物理值优先展示，分数永远附在原始值旁
- 公开参照表及换算公式（[design.md](design.md)）
- 总分采用几何平均，避免单项霸权
- 所有工作负载参数硬编码并公开

## 许可

本项目基于 MIT 许可证开源。第三方组件声明见 [THIRDPARTY-NOTICES.md](THIRDPARTY-NOTICES.md)。