# M1 收尾记录（Closeout）

- **里程碑**: M1 硬件检测 — 完成
- **日期**: 2026-08-17
- **Tag**: `v0.1.0-m1`（指向含最终修复波的提交）
- **分支**: `feature/m1`

## 交付物

- 源码：`src/HardwareBench.{Core,App,Tests}`（10 个 TDD 任务，三工程单仓）
- 发布：`publish.ps1` → 单文件自包含 exe（win-x64，约 155MB）
- 文档：`README.md` / `THIRDPARTY-NOTICES.md` / `design.md` / `docs/plans/2026-08-17-m1-detection.md`
- QA 证据：`docs/qa/m1/startup.png`、`docs/qa/m1/publish-run.png`

## 验证证据

- 测试：**17/17 通过**，构建**零警告**（`TreatWarningsAsErrors` 常开）
- 任务级审查：10/10 通过（每任务独立审查者 + 修复循环）
- 最终全分支审查 → 修复波 `b35da47` → 范围性复审 **3/3 ADDRESSED，无新破坏**
- 真实表面：发布 exe 启动存活 8s+，截图留证

## 最终审查裁定记录

- Critical「DeviceLocator 硬编码空串」→ **误报 PARK**：反射实证（绑定仓库真实依赖图）Hardware.Info 110.0.0.1 的 `Memory` 类型**无** `DeviceLocator` 属性，空串为 Task 7 已文档化适配
- 修复波落实：① `STORAGE_PROPERTY_QUERY` 布局修正（QueryType 写入 query[4..7] 小端，对齐 winioctl.h）② WMI OS 段失败存活测试 + `Assert.All` ③ `PeripheralRegistryDetector` 补取消令牌检查

## M2 待办（延迟 Minor，最终审查裁定 ACCEPT）

1. ~20 文件缺尾随换行；三 csproj 与 `Directory.Build.props` 冗余属性清理
2. `Memory.FormFactor.ToString()` 类型核实（若库类型为 string 则去掉 ToString）
3. 内存插槽位置缺失：库不暴露 → M2 直连 WMI `Win32_PhysicalMemory.DeviceLocator` 补齐
4. DetectionPage 节标题硬编码 → 资源键化（i18n 债；计划模板自身不一致所致）
5. 发布体积 154.7MB（设计投影 40-60MB 为估计非约束）→ 评估 PublishTrimmed / ReadyToRun / 单文件压缩

## 遗留开放项

- GitHub + Gitee 双远端推送：**待老板提供仓库 URL**（Task 10 Step 6 按计划跳过）
