# CAD检查助手

## 项目介绍

CAD检查助手是一款基于 AutoCAD .NET API 开发的 CAD 图纸自动检查工具。

主要用于机械设计图纸检查，通过读取 DWG 文件中的：

- 标题栏信息
- 项目号信息
- 修改记录表
- 图纸布局结构

自动检测图纸规范问题，并提供：

- 自动检查结果输出
- CAD绿色框错误定位
- 批量DWG检查
- CSV检查报告
- 批量清除检查标记

---

# 一、当前版本功能

## v1.1 Batch Check Stable

当前已完成：

### 1. 单张图纸检查

支持：

- 项目号检查
- 修改记录检查
- 缺失字段检测


检查结果：

例如：

类型：
修改记录检查

对象：
标记12

问题：
缺少签名


---

### 2. CAD绿色标记

检查发现问题后：

自动在对应位置生成：
REVISION_CHECK


图层。

使用绿色矩形框标记：

- 缺少签名
- 缺少日期
- 缺少修改内容


---

### 3. 批量DWG检查

支持：

选择文件夹：
文件夹
|
├── A.dwg
├── B.dwg
└── C.dwg


自动执行：


读取DWG
↓
项目号检查
↓
修改记录检查
↓
生成绿色标记
↓
保存DWG
↓
生成报告



---

### 4. 批量检查报告

生成：


CAD检查报告_xxxxxx.csv


包含：

|字段|说明|
|-|-|
|文件路径|DWG完整路径|
|文件名|图纸名称|
|检查类型|项目号/修改记录|
|检查对象|错误位置|
|当前值|读取内容|
|标准值|要求内容|
|结果|失败/通过|
|错误说明|具体问题|

---

### 5. 批量清除标记

支持：

当前图纸清除：


清除当前图纸修改注释


批量清除：


清除所有图纸修改注释


删除：


REVISION_CHECK


图层中的检查框。

---

# 二、项目架构


整体结构：


Forms
↓
Batch/Core
↓
Checks
↓
Readers
↓
Models



---

# 三、目录说明


## Batch

批量处理模块。


### BatchCheckerManager.cs

批量检查核心。

负责：

- 搜索DWG文件
- 调用检查核心
- 保存修改
- 返回检查结果


调用：


BatchCheckForm
|
↓
BatchCheckerManager




---

### BatchMarkerCleaner.cs

批量清除绿色检查框。


流程：


选择文件夹

↓

打开DWG数据库

↓

删除REVISION_CHECK对象



---

### DrawingWeightCalculator.cs

批量检查进度计算。


作用：

根据：

- 文件大小
- Layout数量
- CAD对象数量

估算图纸处理权重。


用于：

真实进度显示。


---

# Checks


检查规则模块。


## ProjectChecker.cs

项目号检查。

负责：

判断：

文件名项目号

是否与：

图纸内部项目号

一致。



---

## RevisionChecker.cs

修改记录检查核心。


负责：

检查：

- 更改内容
- 更改日期
- 签名


---

## RevisionIssueMapper.cs

问题坐标映射。


作用：

将：

检查结果

转换为：

CAD中的标记位置。


---

# Command


AutoCAD命令入口。


## CommandEntry.cs

插件加载入口。


负责：

启动窗口。


---

## BatchTestCommand.cs

批量功能测试命令。


开发阶段使用。


---

## TitleTestCommand.cs

标题栏读取测试命令。


开发阶段使用。


---

# Core


核心业务逻辑。


## DrawingCheckManager.cs


整个检查系统核心。


执行：


项目号读取

↓

项目号检查

↓

布局读取

↓

修改记录读取

↓

错误定位

↓

生成标记



单张和批量检查最终都会调用这里。


---

# Export


输出模块。


## BatchCsvExporter.cs

批量检查报告生成。


输入：


List<CheckResult>


输出：


CSV文件



---

## TitleCsvExporter.cs

标题栏信息导出。


用于后续：

- 数据统计
- 批量信息整理


---

## CsvExporter.cs

旧版CSV导出类。


当前正式流程未使用。


保留原因：

历史代码参考。


后续可删除。


---

# Markers


CAD标记模块。


## RevisionMarker.cs

绿色框生成与清除核心。


负责：

创建：


REVISION_CHECK


图层。


绘制：

Polyline矩形框。


关联：


DrawingCheckManager

BatchMarkerCleaner

CheckForm

BatchCheckForm



---

## ErrorMarker.cs

旧版错误标记。


当前主要使用：


RevisionMarker


暂未删除。


---

# Models


数据模型。


用于模块之间传递数据。


主要：

## CheckResult.cs

检查结果。


包含：

- 文件路径
- 文件名
- 检查类型
- 错误信息


---

## RevisionInfo.cs

修改记录信息。


---

## RevisionLocation.cs

修改记录坐标。


用于：

绿色框定位。


---

## RevisionMarkPoint.cs

绿色框绘制点。


---

## LayoutInfo.cs

布局信息。


---

## BatchReportInfo.cs

保存最近一次批量报告路径。


用于：

打开最近报告。


---

# Readers


数据读取模块。


负责：

从DWG中读取信息。


---

## ProjectReader.cs

读取项目号。


支持：

- DBText
- MText
- Block内部文字


---

## LayoutReader.cs

读取：

- Layout
- BlockTableRecord
- 图纸范围


---

## RevisionTableReader.cs

读取修改记录表。


支持：

- 横版修改记录
- 竖版修改记录


---

## RevisionLocationReader.cs

读取修改记录坐标。


---

## TitleBlockReader.cs

标题栏读取。


---

# Forms


界面模块。


## CheckSelectForm

检查入口选择窗口。


用于：

选择：

- 单张检查
- 批量检查


---

## SingleCheckForm

单张检查窗口。


---

## BatchCheckForm

批量检查窗口。


功能：

- 执行批量检查
- 打开报告
- 清除标记


---

## BatchProgressForm

批量检查进度窗口。


显示：

- 当前DWG
- 完成百分比


---

# 四、开发规范


## 1. 新增检查规则

不要直接修改窗体。


正确流程：


Readers
↓
Checks
↓
Core
↓
Forms



例如：

增加新检查：

新增：


Checks/NewChecker.cs



然后在：


DrawingCheckManager.cs


调用。


---

## 2. 不要在Reader里面写检查逻辑

Reader：

只负责读取。


例如：

正确：


ProjectReader
↓
读取项目号



错误：


ProjectReader
↓
判断项目号是否正确



判断应该放：


ProjectChecker



---

## 3. 不要在Form里面写CAD逻辑

Form只负责：

- 用户操作
- 调用功能
- 显示结果


CAD处理应该放：


Core
Batch
Readers
Markers



---

# 五、Git版本记录


当前建议版本：


v1.1-batch-check-stable



已完成：

- 单张检查
- 批量检查
- 绿色标记
- 批量清除
- CSV报告
- 批量进度框架


---

# 六、后续开发计划


## 短期

- CSV文件路径超链接
- 优化检查报告格式
- 优化窗口布局
- 增加日志


---

## 中期

增加检查：

- 标题栏完整性
- 图框比例检查
- 图层规范检查
- 尺寸标注检查


---

## 长期

建立：

图纸质量检查平台。


支持：

- 批量数据库
- Web管理
- 检查历史记录
- 自动统计


---

# 七、维护说明


接手开发时：

推荐阅读顺序：

1. README.md

↓

2. CommandEntry.cs

↓

3. CheckSelectForm.cs

↓

4. DrawingCheckManager.cs

↓

5. Readers

↓

6. Checks

↓

7. Markers


不要直接修改UI代码。


先理解：


读取
↓
检查
↓
结果
↓
标记
↓
输出


整个流程。
