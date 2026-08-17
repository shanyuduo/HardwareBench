# M2 收尾记录（Closeout）

- **里程碑**: M2 跑分闭环 — 完成
- **日期**: 2026-08-17
- **Tag**: `v0.2.0-m2`
- **分支**: `feature/m2`（15 个提交 + 1 个收尾提交）

## 交付物

- **引擎层**（`src/HardwareBench.Core/Benchmarks/`）：模型+JSON / SHA256 校验的工具内嵌提取（安全铁律：仅内嵌资源、执行前校验、写入仅限 `%TEMP%\HardwareBench\`）/ Job Object 受限子进程执行器 / 三套 fixture 实证解析器（DiskSpd/7-Zip/STREAM）
- **三引擎**：DiskSpd v2.2（顺序+4K 随机）、7-Zip 7zr（压缩/解压/总分）、STREAM 5.10 Windows 变体（Copy/Scale/Add/Triad）——参数硬编码公开
- **公平性守卫**：电源方案检查 + GetSystemTimes 后台 CPU 采样，警告全链路传播（guard→service→model→UI）
- **评分**：公开版本化参照表 `2026.08-m2`（`docs/reference-table.md`）+ 中位数取值 + 几何平均总分
- **编排**：预热+3 轮取中位数、逐引擎容错、OCE 全链路传播
- **UI/CLI**：跑分页（进度/公平性提示/结果表/总分）+ 无头模式 `--benchmark [--export <path>]`
- **资产**：三工具二进制+许可+SHA256 清单内嵌（`assets/tools/`），STREAM 许可条款 3b variant 声明 + 上游/修改源码入库
- **验收证据**：`docs/qa/m2/reproducibility.md` + `run1/run2.json`

## 验证证据

- 测试 **76/76** 绿（69 单元 + 7 集成），构建零警告（TreatWarningsAsErrors 常开）
- 发布 exe 156.0MB（SHA256 `7411DECF…441BD9D`，`publish.ps1` 产出）
- **复现性验收 PASS**（里程碑验收线 <5%）：发布 exe 无头双轮全链路（各 ~3.3 分钟，exit 0），四项评分指标偏差 disk-seq-read **2.32%** / disk-4k-read **2.06%** / cpu-7z-rating **3.60%** / mem-triad **1.98%**——终审者从原始 JSON 独立重算核实
- 审查链：10 任务级审查 + 3 个修复波（工具哈希截断×2 / Job assign 泄漏 / fairness 警告传播）+ 终审 + 终审修复波，全部复审通过

## 终审裁定记录

- `ui-startup.png` 原图为实现者全桌面截图误捕的**英雄联盟对局画面**（含用户隐私）→ 终审 Critical → 窗口域 PrintWindow 重捕（已验证为真实应用窗口）+ **推送前历史重写**（filter-branch 替换 blob，最终树逐位一致 `67a1a37e`，旧 blob 已从对象库清除，tag 在新历史上重建）
- run1/run2.json 补入库（reproducibility.md 证据链闭合）
- `RunWarmupAsync` 死代码删除（编排器接口纯净，-d3 快速预热显式弃权，成本 +2s）
- 7zr LGPL §6 源码可得性链接补入 THIRDPARTY-NOTICES
- 无头退出码推断（run1 未直捕）裁定为代码路径保证：JSON 写入是 Shutdown(0) 前最后一步、异常路径必写 .error.txt + Shutdown(1)——完整 JSON + 无错误文件 = exit 0 可证

## 模型替代记录（诚实披露）

本里程碑执行中期 GLM-5.3 与 k3-coder 子代理池余额耗尽（"Insufficient Balance" ×4），Task 3 实现者会话中断后由控制器修复一行编译错误并接管验证；此后全部实现/审查/修复经 `quick` 档路由至 **deepseek-v4-flash** 执行（金丝雀探测确认路由存活）。账本逐条记录；GLM 池恢复后 M3 可切回。

## M3 待办（延迟 Minor，终审裁定 ACCEPT）

1. ~30 文件缺尾随换行（批量 .editorconfig/dotnet format 清理）
2. GUI 模式公平性采样 Thread.Sleep(300) 冻结 UI 线程 ~300ms → 改 Task.Delay
3. Scoring (int) 强转先于 Clamp 的理论溢出边；重复 metric id 未检测；Culture 不变量的 VM 显示格式
4. `--export` 为末参数被静默忽略；GetExportPath 移入 try
5. ToolExtractor 跨进程 .tmp 竞态；BenchmarkResult.Environment 反序列化 null! 边
6. M3 主体：GPU 引擎（Blender headless）、SQLite 历史结果、LHM 可选传感器模式（默认关+管理员）

## 遗留开放项

- GitHub PR：本收尾提交后创建
- Gitee 双推送：**待老板提供 40 位私人令牌**（凭证管理器存量凭证 API 401，M1 起挂起）
