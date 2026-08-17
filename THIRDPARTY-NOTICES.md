# 第三方组件声明

| 组件 | 许可证 | 用途 |
|---|---|---|
| Hardware.Info (Jinjinov) | MIT | WMI 硬件信息查询 |
| CommunityToolkit.Mvvm | MIT | MVVM 基础设施 |
| Microsoft.Extensions.Hosting | MIT | 依赖注入容器 |
| Microsoft.Win32.Registry (.NET Foundation) | MIT | 注册表 EDID/外设访问 |
| Microsoft DiskSpd v2.2 | MIT | 磁盘基准引擎（内嵌分发，`assets/tools/diskspd/`） |
| 7-Zip 7zr.exe | LGPL-2.1（含 unRAR 限制） | CPU 压缩基准引擎（内嵌分发，`assets/tools/7zr/`） |
| STREAM benchmark (John McCalpin) | 自定义许可（允许使用，结果须符合 Run Rules） | 内存带宽基准引擎（内嵌分发，`assets/tools/stream/`） |
| .NET 运行时与 WPF | MIT | 运行时框架 |

## 内存基准的 variant 声明

按 STREAM 许可条款 3b：本项目的 Windows 版 STREAM 基于**修改过的源码**（`unistd.h`/`sys/time.h` 的 `_WIN32` 守卫、`QueryPerformanceCounter` 替代 `gettimeofday`、`ssize_t` typedef），属 *"based on a variant of the STREAM benchmark code"*。修改仅限 Windows 平台适配，不改变任何内核算法与数据量。上游原版与修改版源码均在 `assets/tools/stream/`。

完整许可文本见各组件发行包。