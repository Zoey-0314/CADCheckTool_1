# CADCheckTool 通用化接入指南

本文用于把 `generalized-tool` 分支接入新的公司标准或图纸模板。该分支保留 2.2.0 的现有判断流程，只把公司专用信息替换为中性示例；自动识别模板、外置规则和取消硬编码不在本次改动范围内。

## 一、当前中性示例约定

| 项目 | 本分支示例 | 说明 |
| --- | --- | --- |
| 非标件图号前缀 | `AB`，例如 `AB386DY` | 替代公司专用前缀；实际部署前按本单位规则修改 |
| 项目号 | `P` + 4 位数字 + 2 位字母 + 3 位数字，例如 `P2026AB001` | 可带 `-L0`、`-PE1` 等后缀 |
| 通用版本 | `V` + 数字 | 用于不带项目号的归档图 |
| 项目版本 | `L` + 数字 | 用于带项目号的归档图 |
| 图纸范围默认路径 | `（所查图纸范围）` | 说明性占位值，不是真实路径 |
| 标准件库默认路径 | `（标准件比对库）` | 说明性占位值，不是真实文件 |

修改命名规则时必须成组修改，不能只改界面文字或一个正则。读取器、检查器、归档索引和快速划改使用相同规则，漏改一处会造成同一件号在不同功能中分类不一致。

## 二、部署前必须确认的配置

### 1. 路径和外部数据

| 内容 | 修改位置 | 核对事项 |
| --- | --- | --- |
| 默认非标归档目录 | `Configs/AppPathConfig.cs` | `DefaultNonStandardArchivePath`；也可在界面“路径设置”中为每位用户保存 |
| 默认标准件库 | `Configs/AppPathConfig.cs` | `DefaultStandardPartDatabasePath` 必须指向可读取的 `.xlsx` |
| 默认版本归档目录 | `Configs/AppPathConfig.cs` | `DefaultVersionArchivePath` |
| 用户配置文件 | `%APPDATA%\Correct_test1\AppPathSettings.json` | 每位 Windows 用户独立保存；切换测试环境时不要沿用旧路径 |
| 标准件 Excel 列 | `Readers/StandardPartExcelReader.cs` | 第一张工作表，第 1 行为表头；第 2 行开始依次读取名称、出口件号、国标件号、用途四列 |

部署前应分别验证本地路径、映射盘和 UNC 路径的访问权限。安装在 `Program Files` 不会改变用户配置位置。

### 2. 图号和非标件规则

| 规则 | 主要文件 | 必须保持一致的内容 |
| --- | --- | --- |
| 非标件分类 | `Core/PartNumberTypeClassifier.cs` | `StartsWith("AB")` |
| 归档文件名取图号 | `Core/NonStandardArchiveIndex.cs` | `DrawingNumberRegex` 中的 `AB` 前缀 |
| BOM 非标件号拆分 | `Readers/NonStandardPartNumberLayoutReader.cs`、`Checks/NonStandardPartNumberChecker.cs` | 前缀、末尾序号、下划线件号的拆分规则 |
| 非标归档搜索键 | `Checks/NonStandardArchiveChecker.cs` | 图号裁剪和归档匹配规则 |
| 快速划改是否写项目号 | `QuickRevision/Models/RevisionTarget.cs` | 原 BOM 内容是否以 `AB` 开头 |
| 一般图号格式 | `Configs/BomConfig.cs` | `DrawingNumberPattern` 与 `DrawingNumberSuffix` |

如果实际存在多个非标前缀，当前代码不能只改一个示例值解决，应先定义明确的多前缀分类规则，再同步归档索引、BOM 拆分和快速划改。

### 3. 项目号、版本号和文件名

当前示例项目号正则为 `P\d{4}[A-Z]{2}\d{3}`，可带字母数字后缀。相关位置包括：

- `Readers/FileNameProjectReader.cs`：从 DWG 文件名读取项目号。
- `Readers/BomProjectNumberReader.cs`：读取 BOM 右侧项目号。
- `Readers/ProjectReader.cs`：读取图纸中的项目号。
- `Core/NonStandardArchiveIndex.cs`：建立图号与项目号联合索引。
- `VersionCheck/Readers/DrawingVersionReader.cs`：读取当前图纸版本。
- `VersionCheck/Core/VersionArchiveIndex.cs` 和 `VersionCheck/Services/VersionCheckService.cs`：解析归档文件名并比较最新版本。
- `ProjectVersion/Writers/ProjectVersionWriter.cs`：校验要写入图纸的项目版本。

修改项目号结构时，应准备“纯项目号、带 L 版本、带业务后缀、空格分隔、下划线分隔、非法格式”六类样例。版本比较还要验证 `V9/V10`、`L9/L10`，避免按字符串顺序比较。

### 4. 标题栏和图框模板

标题栏不是按块名自动识别，而是按图框方向和固定区域取文字：

- `Configs/TitleBlockHorizontalConfig.cs`：横版标题栏各字段的 `MinX/MaxX/MinY/MaxY`。
- `Configs/TitleBlockVerticalConfig.cs`：竖版标题栏各字段的区域。
- `Core/TitleBlockOrientationDetector.cs`：横版、竖版方向判断。
- `Readers/DrawingFrameReader.cs`：图框读取。
- `Readers/TitleBlockReader.cs`、`Readers/TitleBlockRegionParser.cs`：区域内文字读取和字段映射。
- `Checks/TitleBlockCheckManager.cs`、`Checks/TitleBlockChecker.cs`：字段完整性和图号一致性检查。

新模板应先在 AutoCAD 中用 `LIST` 或开发调试输出确认图框基点、比例、旋转和标题栏字段边界。横版和竖版各准备至少一张正常图与一张故意缺字段的图。不要只按屏幕像素估算坐标。

### 5. BOM 模板和图层

`Configs/BomConfig.cs` 集中保存常见 BOM 图层、表头别名和一般图号正则。接入时必须确认：

- BOM 位于 AutoCAD `Table`、普通线文字组合还是块属性中。
- 图层是否属于 `0`、`BOM`、`BOM_TABLE`，否则补充实际图层。
- 序号、图号、名称、数量的表头文字是否包含在对应别名列表。
- 数量表头是否确实使用 `Qut.`；不同模板常见 `Qty.`，需要按实物补充。
- 单元格合并、换行、空行和多页 BOM 是否与现有读取逻辑一致。
- BOM 序号引出、焊接符号排除所用的线、文字和视口坐标是否可读。

涉及几何阈值时，重点查看 `Readers/LayoutReader.cs`、`Readers/ViewportLineReader.cs`、`Checks/BomCalloutChecker.cs` 和 `Checks/BomTableRecognizer.cs`。修改容差前必须保留原样图回归，避免修复一种模板后误伤另一种模板。

### 6. 修改记录、标记和项目版本位置

| 内容 | 主要文件 | 适配要点 |
| --- | --- | --- |
| 修改记录表 | `Readers/RevisionTableReader.cs`、`Readers/RevisionLocationReader.cs`、`Checks/RevisionChecker.cs` | 表格区域、版本列、日期和说明字段 |
| 项目版本标注 | `ProjectVersion/Configs/ProjectVersionConfig.cs` | 横版/竖版的 X、Y、字高、宽度、搜索容差和文字样式 |
| 检查标记 | `Configs/MarkerConfig.cs`、`Markers/MarkerManager.cs` | 图层名、颜色、字高、矩形尺寸和偏移 |
| 快速划改 | `QuickRevision` 目录 | 目标类型、视口坐标转换、删除线、新文字和项目号落点 |

所有位置类对象的 `LayoutName`、`ViewportId`、`SpaceId` 和 `Position` 都参与后续标记定位。适配时不能为了简化数据而删除这些字段。

### 7. 安装包和品牌信息

- `Installer/PackageContents.xml`：应用名、作者、公司信息、AutoCAD 版本范围和自动加载命令。
- `Installer/CADCheckTool_v2.2.0.iss`：产品名、发布者、版本、安装范围和输出文件名。
- `Properties/AssemblyInfo.cs`：程序集标题、公司、产品和版本。
- `README.md`、`LICENSE`：发布地址、维护者说明和许可证归属。

本分支使用 `CADCheckTool Maintainers` 作为中性发布者。正式对外发布前应替换为真实维护主体；许可证署名属于法律信息，不应机械删除。

## 三、最小回归样图集

每次适配至少保留以下去敏样图，并记录预期结果：

1. 横版和竖版标题栏各一张，字段完整。
2. 缺图号、缺项目号、页码错误、文字高度错误各一张。
3. 含标准件、AB 非标件、下划线件号和不存在归档件号的 BOM。
4. BOM 序号正常、漏标、多标以及邻近焊接符号的图纸。
5. 通用 `V` 版本、项目 `L` 版本、当前已最新和当前落后各一张。
6. 模型空间、图纸空间、视口内文字和表格单元格的快速划改样例。
7. 批量保存样例，包括只读文件、无权限目录和异常 DWG。

回归时不仅看报表，还要检查标记是否落在正确布局、正确视口和正确对象附近，并验证原 DWG、临时文件和 `.bak` 的安全保存流程。

## 四、本次没有改动的内容

- 没有重写标题栏、BOM 或焊接符号的识别算法。
- 没有删除模型空间、布局、视口和表格单元格支持。
- 没有改变批处理的文档生命周期、临时保存、校验、备份和替换流程。
- 没有把模板坐标、正则和几何容差改成外部配置。

## 五、后续减少硬编码的方向

后续可按风险从低到高推进：

1. 将前缀、项目号正则、版本规则、BOM 图层和表头别名移到带版本号的 JSON 规则文件。
2. 增加“模板校准”命令，由维护者在 AutoCAD 中框选标题栏和修改记录区域，保存坐标配置。
3. 优先读取标题栏块属性标签和 AutoCAD Table 表头；只有读取不到结构化数据时才回退到固定坐标。
4. 为不同公司或模板建立独立规则配置档，启动检查时按块名、图框尺寸或用户选择加载。
5. 对几何聚类和模板识别输出置信度；低置信度时提示人工确认，不自动判错。

在上述机制建立前，规则调整仍应通过独立分支、去敏样图集和完整回归后发布。
