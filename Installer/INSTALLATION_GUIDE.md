# CADCheckTool_1 v1.5.2 安装与卸载指南

## 安装流程

1. 关闭所有正在运行的 AutoCAD 实例。
2. 右键以管理员身份运行 `CADCheckTool_1_Setup.exe`。
3. 选择安装路径；默认路径由安装程序动态提供。
4. 安装程序将部署以下目录结构：

```text
CADCheckTool_1
├── CADCheckTool_1.dll
├── Resources
│   └── StandardParts.xlsx
├── Configs
└── Logs
```

5. 安装程序为当前 Windows 用户已初始化的每个 AutoCAD 产品配置自动加载注册表项：
   - `DESCRIPTION`: `CADCheckTool_1 AutoCAD Engineering Drawing Inspection Plugin`
   - `LOADER`: 已安装 DLL 的完整路径
   - `LOADCTRLS`: `15`
   - `MANAGED`: `1`
6. 启动 AutoCAD 后，插件会自动加载，不需要执行 `NETLOAD`。

> 安装程序应通过自定义操作调用 `CADCheckToolInstaller.Install()`，并将安装包解压目录和用户选择的安装目录传给 `CADCheckToolInstaller` 构造函数。安装程序需要提升权限，以便写入受保护的安装目录。

## 卸载流程

- 在 Windows 控制面板中选择“卸载 CADCheckTool_1”，或运行安装包提供的卸载程序。
- 卸载程序应调用 `CADCheckToolInstaller.Uninstall()`。
- 卸载会移除 CADCheckTool_1 的 AutoCAD 自动加载注册表项和安装目录。
- 安装目录中的 `Configs` 与 `Logs` 会先备份至当前用户的应用数据目录下的 `CADCheckTool_1/UninstallBackups`，避免误删用户配置和日志。

## 验收测试

1. 删除已有的 CADCheckTool_1 自动加载注册表项，重新安装后启动 AutoCAD，确认插件自动加载。
2. 关闭并重新启动 AutoCAD，确认整个过程不需要执行 `NETLOAD`。
3. 卸载后启动 AutoCAD，确认 CADCheckTool_1 不再加载。
4. 重新安装后，确认 BOM 检查、标准件检查、Marker、单张检查和批量检查均正常可用。

## 故障排查

如果插件没有自动加载，请依次检查：

- **AutoCAD 初始化状态**：自动加载项写入当前用户的 AutoCAD 注册表。若目标用户从未启动过 AutoCAD，请先启动并关闭一次 AutoCAD 后重新执行安装。
- **注册表**：在当前用户的 `Software/Autodesk/AutoCAD` 下，确认相应 AutoCAD 产品的 `Applications/CADCheckTool_1` 项存在。
- **DLL 路径**：确认 `LOADER` 的值是现有的 `CADCheckTool_1.dll` 完整路径，且文件未被移动或删除。
- **文件权限**：确认目标用户对安装目录有读取权限；安装或更新到受保护目录时，请使用提升权限的安装程序。
- **依赖文件**：安装包必须包含插件运行所需的全部第三方依赖程序集，且这些程序集应与插件 DLL 位于可解析的位置。

安装与卸载日志使用 `[Installer]` 前缀记录，可在当前用户的应用数据日志目录中查看。
