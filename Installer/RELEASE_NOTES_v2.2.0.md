# CADCheckTool 2.2.0 Release Notes

## 交付范围

- 公司维护版只通过 `company-maintenance-v2.2.0` 的 GitHub Actions artifact 分发，不创建公开 Release。
- 公开 Release 中的 `v2.2.0-generalized` 是通用化 pre-release，不适用于公司生产图纸。
- 兼容 AutoCAD 2024（R24.3），Windows x64，.NET Framework 4.8。
- 提供单一 Inno Setup 安装器。
- 同时提供 `CADCheckTool_1_v2.2.0_InnoSetup_Source.zip`，内含完整运行 DLL、Inno Setup 脚本和本地重新打包脚本，可在替换同一次构建的插件文件后自行生成安装 EXE。
- 支持当前用户免管理员安装和所有用户管理员安装。
- 使用 Autodesk ApplicationPlugins 自动加载，不要求 `NETLOAD` 或手工注册表配置。
- 使用 AutoCAD 标准的 `.Net` 命令按需加载声明，确保安装后可以直接识别 `CHECKDRAWING` 等命令。

## 功能

- 图纸标题栏、BOM、标准件、非标件、序号、修改记录和版本归档检查。
- 单张与批量检查、标记、清除和 CSV 导出。
- 项目版本写入和快速划改命令。
- 批量图纸安全保存及日志记录。

## 交付质量

- 全部源码、安装脚本和交付文档统一为 UTF-8，发布流水线检查乱码特征和关键中文字符串。
- 删除旧注册表安装器、InstallerLauncher、测试命令、补丁脚本、无引用配置和过期网页。
- bundle 采用运行时文件白名单，不包含 PDB、XML API 文档、AutoCAD API DLL 或源码。
- ZIP 同时提供 SHA-256 校验文件和最新安装说明。

## 已知边界

- 不支持 AutoCAD 2025 及更高版本；这些版本需要 .NET 8 专用构建。
- 企业网络目录、映射盘和标准件 Excel 的访问权限由用户所在环境提供。
