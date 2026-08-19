# CADCheckTool 配置指南

本文对应 v2.2.0。配置文件由程序生成，不随安装包分发，也不应放进 Program Files。

## 用户路径配置

配置文件：`%APPDATA%\Correct_test1\AppPathSettings.json`

首次运行时会使用 UTF-8 自动创建：

```json
{
  "NonStandardArchivePath": "Z:\\归档图纸",
  "StandardPartDatabasePath": "Z:\\图号管理\\诺升标准件统一命名.xlsx",
  "VersionArchivePath": "Z:\\归档图纸"
}
```

| 字段 | 用途 | 默认值 |
| --- | --- | --- |
| `NonStandardArchivePath` | 非标归档图纸根目录 | `Z:\归档图纸` |
| `StandardPartDatabasePath` | 标准件 Excel 数据库 | `Z:\图号管理\诺升标准件统一命名.xlsx` |
| `VersionArchivePath` | 最新版本检查的归档目录 | `Z:\归档图纸` |

推荐在 `CHECKDRAWING` 主界面的“路径设置”中修改。直接编辑 JSON 时必须保持合法 JSON 和 UTF-8 编码；保存后重新打开插件界面或重启 AutoCAD。

## 外部数据要求

### 标准件 Excel

- 文件必须是可读取的 `.xlsx`。
- 当前 Windows 用户需要文件和目录读取权限。
- 表格中的件号会经过空白、大小写和部分格式归一化后匹配。
- 更新 Excel 后，下一次加载会按文件状态刷新缓存。

### 非标与版本归档

- 路径可以是本地目录、映射盘或 UNC 路径。
- 批量检查所使用的 AutoCAD 进程必须能访问该路径。
- 无法访问时会记录日志并生成相应“未执行”或“未找到”结果，不应通过复制归档数据到安装目录解决。

## 内置规则配置

以下规则随程序集发布，修改后必须重新构建和回归测试：

| 文件 | 内容 |
| --- | --- |
| `Configs/BomConfig.cs` | BOM 表头、列和识别相关常量 |
| `Configs/MarkerConfig.cs` | 标记图层、颜色、尺寸和闭合状态 |
| `Configs/TitleBlockHorizontalConfig.cs` | 横向标题栏区域与字段规则 |
| `Configs/TitleBlockVerticalConfig.cs` | 纵向标题栏区域与字段规则 |

不要在交付后直接修改 DLL 或 `PackageContents.xml` 来调整业务规则。

## 日志

正常日志：`%APPDATA%\Correct_test1\Logs\yyyy-MM-dd.log`

若用户目录不可写，日志会尝试写入系统临时目录下的 `Correct_test1_fallback.log`。排查路径访问、图纸读取或批量保存问题时，应同时提供当天日志、AutoCAD 版本和问题 DWG 的最小复现样本。

## 安装位置与配置的关系

- 仅为我安装：插件在 `%APPDATA%\Autodesk\ApplicationPlugins`。
- 为所有用户安装：插件在 `%ProgramFiles%\Autodesk\ApplicationPlugins`。
- 两种模式都从 `%APPDATA%\Correct_test1` 读取当前 Windows 用户的配置和日志。

因此 IT 为所有用户安装插件后，每位用户仍可以拥有独立路径配置。
