# Configuration Guide

## Purpose

CADCheckTool_1 is an AutoCAD plugin for mechanical engineering drawing
inspection.

Different companies may have different:

-   Drawing number rules
-   Project number formats
-   Title block templates
-   BOM standards
-   Standard part databases
-   Marker styles

Therefore, some rules must be customized before deployment.

------------------------------------------------------------------------

# 1. Drawing Number Rules

## Description

The system checks consistency between:

-   DWG file information
-   Title block information

Example:

File:

    ABC-001.dwg

Title block:

    ABC-002

Result:

    Drawing Number Mismatch

## Configuration Location

    Checks/
    Readers/
    Configs/
    Models/

Customizable parameters:

-   Prefix format
-   Number length
-   Separator
-   Revision suffix
-   Regular expression rules

Example:

Company A:

    ABC-001

Company B:

    PRJ_2026_001

------------------------------------------------------------------------

# 2. Project Number Rules

Project number formats are company-specific.

Possible storage locations:

-   Title block
-   File name
-   DWG properties
-   Custom attributes

Configuration locations:

    Checks/
    Models/
    Configs/

Possible parameters:

-   Prefix
-   Length
-   Regular expression
-   Position

------------------------------------------------------------------------

# 3. Title Block Configuration

Title block layouts vary between companies.

Configuration files:

    Configs/

    TitleBlockConfig.cs
    TitleBlockHorizontalConfig.cs
    TitleBlockVerticalConfig.cs

Modify when:

-   Template changes
-   Title block moves
-   Attribute names change
-   Block structure changes

------------------------------------------------------------------------

# 4. Revision Configuration

Revision checking parameters are stored in:

    Configs/

Possible customization:

-   Detection area
-   Table range
-   Marker size
-   Layer name
-   Tolerance

------------------------------------------------------------------------

# 5. BOM Configuration

The system supports automatic BOM extraction and checking.

Workflow:

    DWG

    ↓

    CadTableReader

    ↓

    BomTableRecognizer

    ↓

    BomData

    ↓

    Inspection

Configuration locations:

    Readers/
    Checks/
    Configs/

Customizable items:

-   BOM header names
-   Column mapping
-   Part number field
-   Name field
-   Quantity field

Default headers:

    No.
    Part No.
    Name
    Qut.

------------------------------------------------------------------------

# 6. Standard Part Configuration

The system verifies standard parts using a standard part database.

Database:

    Resources/

    StandardParts.xlsx

Data fields:

  Field         Description
  ------------- ----------------------
  Part Number   Standard part number
  Name          Standard part name

## Matching Strategy

The system uses two-stage matching.

### Stage 1: Loose Matching

Handles:

-   Space differences
-   Format differences

Example:

    ASME B18.2.1 5/8-11x2 G5

and

    ASME B18.2.1 5/8-11 x 2 G5

can be treated as the same candidate.

### Stage 2: Strict Validation

Checks:

-   Part number format
-   Part name

Results:

    Correct
    FormatDifference
    NameError
    NotRegistered
    MultipleMatch

------------------------------------------------------------------------

# 7. Non-standard Part Rules

Non-standard company parts are excluded from standard part checking.

Default rule:

    NSxxxx

Example:

    NS265R1
    NS135H

Processing:

    NonStandardPart

    ↓

    Skip Standard Part Check

Configuration locations:

    Core/
    Models/
    Configs/

------------------------------------------------------------------------

# 8. Marker Configuration

Markers visualize inspection results inside CAD.

Configuration:

    Configs/

    MarkerConfig.cs

Controls:

-   Layer name
-   Text size
-   Marker size
-   Color
-   Display style

Default layer:

    Correct_test1_Marker

Marker supports:

-   Error location display
-   XData association
-   Safe removal

------------------------------------------------------------------------

# 9. Safe DWG Saving Configuration

The system uses safe saving during DWG modification.

Workflow:

    Original DWG

    ↓

    Temporary File

    ↓

    Verify

    ↓

    Replace

Purpose:

Prevent:

-   Save failure
-   CAD crash
-   File corruption

Related module:

    Core/

    SafeDwgSaver.cs

------------------------------------------------------------------------

# 10. Maintenance Rule

When modifying company-specific rules, add comments:

``` csharp
/*
 Company specific rule.

 Modify when:
 - CAD standard changes
 - Template changes
 - Naming convention changes
*/
```

This helps future developers identify customizable areas.

------------------------------------------------------------------------


## 目的

CADCheckTool_1 是用于机械工程图自动审核的 AutoCAD 插件。

不同企业具有不同的：

-   图号规则
-   项目编号规则
-   标题栏模板
-   BOM标准
-   标准件库
-   标记规范

因此，部署到不同企业前，需要针对实际标准进行配置。

------------------------------------------------------------------------

# 1. 图号规则

## 功能说明

系统检查：

-   DWG文件信息
-   标题栏信息

是否一致。

示例：

文件：

    ABC-001.dwg

标题栏：

    ABC-002

结果：

    图号不一致

## 配置位置

    Checks/
    Readers/
    Configs/
    Models/

可修改：

-   图号前缀
-   数字长度
-   分隔符
-   后缀规则
-   正则表达式

------------------------------------------------------------------------

# 2. 项目号规则

项目号通常属于企业内部规则。

可能来源：

-   标题栏
-   文件名
-   DWG属性
-   自定义属性

配置位置：

    Checks/
    Models/
    Configs/

可配置：

-   前缀
-   长度
-   正则表达式
-   位置

------------------------------------------------------------------------

# 3. 标题栏配置

不同企业CAD模板存在差异。

配置文件：

    Configs/

    TitleBlockConfig.cs
    TitleBlockHorizontalConfig.cs
    TitleBlockVerticalConfig.cs

以下情况需要修改：

-   模板变化
-   标题栏移动
-   属性名称变化
-   块结构变化

------------------------------------------------------------------------

# 4. 修改记录配置

修改记录检查参数位于：

    Configs/

可配置：

-   检测区域
-   表格范围
-   Marker大小
-   图层名称
-   容差

------------------------------------------------------------------------

# 5. BOM配置

系统支持自动读取和检查BOM表。

流程：

    DWG

    ↓

    CadTableReader

    ↓

    BomTableRecognizer

    ↓

    BomData

    ↓

    检查

配置位置：

    Readers/
    Checks/
    Configs/

可配置：

-   BOM表头名称
-   列映射
-   图号字段
-   名称字段
-   数量字段

默认：

    No.
    Part No.
    Name
    Qut.

------------------------------------------------------------------------

# 6. 标准件配置

系统通过标准件数据库检查BOM中的标准件。

数据库：

    Resources/

    StandardParts.xlsx

字段：

  字段          说明
  ------------- ------------
  Part Number   标准件图号
  Name          标准件名称

## 匹配策略

采用两级匹配：

### 一级：宽松匹配

处理：

-   空格差异
-   格式差异

例如：

    ASME B18.2.1 5/8-11x2 G5

和：

    ASME B18.2.1 5/8-11 x 2 G5

认为属于同一候选。

### 二级：严格检查

检查：

-   图号格式
-   零件名称

结果：

    Correct
    FormatDifference
    NameError
    NotRegistered
    MultipleMatch

------------------------------------------------------------------------

# 7. 非标件规则

企业非标件不参与标准件检查。

默认规则：

    NSxxxx

例如：

    NS265R1
    NS135H

处理：

    NonStandardPart

    ↓

    跳过标准件检查

配置位置：

    Core/
    Models/
    Configs/

------------------------------------------------------------------------

# 8. Marker配置

Marker用于在CAD中显示检查结果。

配置：

    Configs/

    MarkerConfig.cs

控制：

-   图层名称
-   文字大小
-   标记尺寸
-   颜色
-   显示方式

默认图层：

    Correct_test1_Marker

支持：

-   错误位置显示
-   XData关联
-   安全清除

------------------------------------------------------------------------

# 9. DWG安全保存配置

系统在修改DWG时使用安全保存机制。

流程：

    原始DWG

    ↓

    临时文件

    ↓

    验证

    ↓

    替换

目的：

防止：

-   保存失败
-   CAD崩溃
-   文件损坏

模块：

    Core/

    SafeDwgSaver.cs

------------------------------------------------------------------------

# 10. 维护规范

修改企业专属规则时，需要添加注释：

``` csharp
/*
 Company specific rule.

 Modify when:
 - CAD standard changes
 - Template changes
 - Naming convention changes
*/
```

帮助后续开发人员识别可配置区域。
