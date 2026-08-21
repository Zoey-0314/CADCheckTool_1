# 开发与发布流程

## 开发基线

- Visual Studio 2022
- .NET Framework 4.8
- `Release|x64`
- AutoCAD 2024 API 24.3
- Inno Setup 6
- 所有文本文件使用 UTF-8；C#、Inno Setup 和中文交付文档使用 UTF-8 BOM

## 修改流程

1. 从最新 `company-maintenance-v2.2.0` 创建公司版短期分支。
2. 只修改当前任务需要的代码；不得提交 `bin`、`obj`、`packages`、临时补丁、调试命令或测试图纸。
3. 搜索调用关系后再删除代码。AutoCAD 命令入口、接口方法和反射入口不能仅按“引用次数少”判断为未使用。
4. 业务规则修改需要更新对应中文维护注释，删除历史叙述、分隔线和被注释掉的旧代码。
5. 同步更新 README、架构、配置、安装说明和 Release Notes。
6. 提交英文 commit，推送分支并创建 Pull Request。

## 本地检查

```powershell
nuget restore packages.config -PackagesDirectory packages -NonInteractive
msbuild Correct_test1.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x64 /p:AutoCADApiDir="C:\Program Files\Autodesk\AutoCAD 2024"
```

还应确认：

- 解决方案只包含 `Correct_test1.csproj`。
- `PackageContents.xml`、程序集版本和安装器版本一致。
- `PackageContents.xml` 仍限定 `R24.3`。
- 源码和文档不存在乱码、替换字符或非 UTF-8 文件。
- 发布目录不包含 AutoCAD API DLL、PDB、XML 文档、测试/补丁文件。

## Pull Request 检查

GitHub Actions 在 Windows 上执行：

1. 全仓文本编码和乱码特征检查。
2. NuGet 依赖还原和 AutoCAD 2024 引用准备。
3. `Release|x64` 编译。
4. 程序集版本、关键中文字符串和依赖白名单校验。
5. ApplicationPlugins bundle 组装。
6. Inno Setup 编译及安装包内容检查。
7. ZIP、安装器和 SHA-256 文件生成。

PR 检查通过后再合并。不要绕过失败的 Windows 构建直接分发安装包。

## v2.2.0 公司维护构建

`company-maintenance-v2.2.0` 的构建流水线只生成 GitHub Actions artifact，不创建公开 tag 或 Release。artifact 名称为 `CADCheckTool_1-v2.2.0-company-build`，包含：

- `CADCheckTool_1_Setup_v2.2.0.exe`
- `CADCheckTool_1_v2.2.0_Windows_x64.zip`
- `CADCheckTool_1_v2.2.0_SHA256.txt`
- `INSTALLATION_GUIDE.md`

构建后下载 artifact 做最终抽检：核对哈希、解压文件白名单、中文显示、普通用户安装、管理员所有用户安装、AutoCAD 2024 自动加载和卸载。

公开 Release 页面中的 `v2.2.0-generalized` 仅为通用化 pre-release。公司维护版不得引用或分发该安装包。

## 后续版本

发布新版本时至少同步修改：

- `Properties/AssemblyInfo.cs`
- `Installer/PackageContents.xml`
- `Installer/CADCheckTool_v2.2.0.iss`（建议新版本同时重命名）
- 工作流文件名、产物名和 Release Notes
- README 中的版本与下载链接

AutoCAD 2025+ 需要单独的 .NET 8 项目和 CI，不得只放宽 `SeriesMax`。
