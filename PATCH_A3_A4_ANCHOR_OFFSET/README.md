# A3/A4 标题栏基准点偏移补丁

基准版本：`v2.1.0-rc3` / `e589e9d196cfaa8736cb1d79f0f173a33575fd32`

## 目标

1. 横竖版优先直接使用图幅文字判断：`A3 = 横版`，`A4 = 竖版`。
2. 使用图幅文字本身作为标题栏坐标基准点。
3. 如果某个 Layout 的标题栏整体发生平移，自动计算 X/Y 偏移，并把现有固定坐标规则同步平移。
4. 不改变现有标题栏字段尺寸、字段相对位置、页码修正、BOM 图号逻辑、字高规则。
5. A3/A4 缺失时保留旧的“标记”文字判断作为兼容回退，不让旧图纸直接失效。

## 基准点

来自当前确认的两张标准图：

- A3 横版：`X = 50.8579`, `Y = 315.3767`, 文字高度约 `5.0`
- A4 竖版：`X = 86.4`, `Y = 350.1487`, 文字高度约 `3.5`

例如某张 A3 图中 A3 实际坐标为 `(55.8579, 312.3767)`，则：

- `OffsetX = +5`
- `OffsetY = -3`

标题栏解析区域、标题栏错误框、图号错误框、项目号/版本号查找和新建位置都会使用同一组偏移。

## 应用

先确保本地已经提交当前修改并拉取最新 master：

```powershell
git checkout master
git pull origin master
git status
```

然后在仓库根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\PATCH_A3_A4_ANCHOR_OFFSET\apply.ps1
```

脚本会在找不到预期旧代码时直接停止，不会静默乱改。

应用后：

```powershell
git diff
dotnet --version
```

本项目仍建议直接在 Visual Studio 中“重新生成解决方案”。确认编译通过后，完全关闭 AutoCAD，重新启动并 NETLOAD 新 DLL。

## 建议测试

至少测试以下 4 类：

- 标准 A3 横版，基准坐标不偏移
- 标准 A4 竖版，基准坐标不偏移
- A3 整体平移的图纸
- A4 整体平移的图纸

重点确认：标题栏字段读取、页码修正、标题栏绿色标记、图号标记、项目号/版本号读取和写入位置。

## 回退

在尚未提交补丁修改时：

```powershell
git restore Core/TitleBlockOrientationDetector.cs Checks/TitleBlockCheckManager.cs Markers/TitleBlockFieldMarker.cs Markers/TitleBlockDrawingNumberMarker.cs ProjectVersion/Configs/ProjectVersionConfig.cs ProjectVersion/Services/ProjectVersionWriteService.cs ProjectVersion/Writers/ProjectVersionWriter.cs VersionCheck/Readers/DrawingVersionReader.cs
```
