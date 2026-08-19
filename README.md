# CADCheckTool 2.2.0

CADCheckTool 是面向 AutoCAD 2024 的工程图纸检查与快速划改插件。正式交付物为一个 Inno Setup 安装程序；安装后由 AutoCAD ApplicationPlugins 机制自动加载，无需 `NETLOAD`，也无需手动写注册表。

## 运行环境

- Windows 10 1809 或更高版本，64 位
- AutoCAD 2024，64 位
- .NET Framework 4.8
- 访问企业归档目录和标准件 Excel 所需的网络权限

> 2.2.0 仅支持 AutoCAD 2024（R24.3）。AutoCAD 2025 及更高版本改用 .NET 8，不在本版本兼容范围内。

## 安装

从 [v2.2.0 Release](https://github.com/Zoey-0314/CADCheckTool_1/releases/tag/v2.2.0) 下载 `CADCheckTool_1_v2.2.0_Windows_x64.zip`，核对 SHA-256 后解压并运行 `CADCheckTool_1_Setup_v2.2.0.exe`。

安装程序提供两种模式：

| 模式 | 权限 | 安装目录 | 适用场景 |
| --- | --- | --- | --- |
| 仅为我安装 | 不需要管理员权限 | `%APPDATA%\Autodesk\ApplicationPlugins\CADCheckTool.bundle` | 普通员工电脑，推荐 |
| 为所有用户安装 | 需要管理员权限 | `%ProgramFiles%\Autodesk\ApplicationPlugins\CADCheckTool.bundle` | IT 统一部署或多人共用电脑 |

安装前必须完全关闭 AutoCAD。详细步骤见 [安装说明](Installer/INSTALLATION_GUIDE.md)。

## 命令

| 命令 | 功能 |
| --- | --- |
| `CHECKDRAWING` | 打开图纸检查主界面，执行单张检查、批量检查、路径设置和版本号输入 |
| `QREV` | 执行一次快速划改 |
| `QREVMODE` | 连续快速划改，按 Esc 结束 |
| `QREVCLEAR` | 清除当前图纸中的快速划改内容 |

## 主要能力

- 标题栏、图号、项目号、页码和文字高度检查
- BOM 表识别、标准件匹配、非标件归档和件号检查
- BOM 序号与图面引出序号一致性检查，并排除焊接符号候选
- 修改记录与版本归档检查
- 单张或批量标记、清除标记和 CSV 导出
- 当前图纸项目版本写入
- 模型空间、图纸空间、视口和表格单元格快速划改
- 批量处理中的安全保存、临时文件验证和日志记录

## 配置与日志

首次运行会创建：

- 配置：`%APPDATA%\Correct_test1\AppPathSettings.json`
- 日志：`%APPDATA%\Correct_test1\Logs\yyyy-MM-dd.log`

路径配置可在插件界面中修改。字段说明见 [配置指南](CONFIGURATION_GUIDE.md)。

## 开发

- Visual Studio 2022
- .NET Framework 4.8
- x64
- AutoCAD 2024 .NET API（24.3）
- NuGet 依赖通过 `packages.config` 还原
- 安装包由 Inno Setup 6 构建

解决方案只包含插件项目 `Correct_test1.csproj`。发布流水线会检查 UTF-8 编码、依赖完整性、中文字符串、包内文件白名单和安装器构建结果。架构与发布要求分别见 [架构说明](ARCHITECTURE.md) 和 [开发流程](DEVELOPMENT_WORKFLOW.md)。

## 版本

当前版本：**2.2.0**

许可证见 [LICENSE](LICENSE)。
