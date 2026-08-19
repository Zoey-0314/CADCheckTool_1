# CADCheckTool 2.2.0 安装说明

## 安装前

1. 确认电脑已安装 64 位 AutoCAD 2024。
2. 确认系统已启用 .NET Framework 4.8。
3. 保存图纸并完全关闭所有 AutoCAD 进程。
4. 从 GitHub Release 下载 ZIP，核对 `CADCheckTool_1_v2.2.0_SHA256.txt` 后解压。

## 选择安装模式

运行 `CADCheckTool_1_Setup_v2.2.0.exe` 后选择：

- **仅为我安装（推荐）**：不需要管理员权限，只为当前 Windows 用户安装。
- **为所有用户安装**：需要管理员权限，供这台电脑上的所有用户使用。没有管理员账户时请选择“仅为我安装”。

安装目录由安装模式自动决定，不能手动修改：

- 当前用户：`%APPDATA%\Autodesk\ApplicationPlugins\CADCheckTool.bundle`
- 所有用户：`%ProgramFiles%\Autodesk\ApplicationPlugins\CADCheckTool.bundle`

安装程序会清理相同范围内的旧 bundle，并移除适用范围内的历史注册表加载项。插件本身不依赖注册表加载。

## 首次使用

1. 启动 AutoCAD 2024。
2. 在命令行输入 `CHECKDRAWING`。
3. 打开“路径设置”，确认非标归档、标准件 Excel 和版本归档路径。

正常情况下无需执行 `NETLOAD`。若命令不存在，先重新启动 AutoCAD，再检查安装目录中是否存在 `CADCheckTool.bundle\PackageContents.xml`。

## 升级与模式切换

安装同一模式的新版本前可直接运行新版安装程序。若从“所有用户”切换到“仅为我”，或反向切换，先在 Windows“已安装的应用”中卸载旧模式，避免两个位置同时存在 bundle。

普通用户无法覆盖或删除 Program Files 中的所有用户版本。如检测到系统范围版本，请联系管理员卸载或使用“为所有用户安装”完成升级。

## 卸载

关闭 AutoCAD，在 Windows“设置 → 应用 → 已安装的应用”中找到 `CADCheckTool 2.2.0` 并卸载。卸载只删除插件文件，不删除 `%APPDATA%\Correct_test1` 中的用户配置和日志。

## 常见问题

### 提示需要管理员账户

重新运行安装程序并选择“仅为我安装”。只有“为所有用户安装”需要管理员权限。

### 中文显示为乱码

只使用 v2.2.0 Release 中的正式安装器，不要混用旧 DLL 或手工复制的历史包。仍有问题时提供安装器文件哈希和截图。

### AutoCAD 正在运行

保存并关闭所有图纸和 AutoCAD 窗口；任务管理器中确认没有 `acad.exe` 后重试。

### 插件未自动加载

确认使用 AutoCAD 2024，而不是 2025/2026；检查 bundle 是否只存在于一个 ApplicationPlugins 目录；随后查看 `%APPDATA%\Correct_test1\Logs`。
