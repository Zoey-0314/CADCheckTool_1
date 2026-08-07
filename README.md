# CADCheckTool_1

AutoCAD engineering drawing automatic inspection plugin.


![AutoCAD](https://img.shields.io/badge/AutoCAD-.NET%20API-blue)
![CSharp](https://img.shields.io/badge/C%23-.NET-purple)
![Version](https://img.shields.io/badge/version-v1.5.0-green)
## Project Introduction

CADCheckTool_1 is a mechanical engineering drawing inspection plugin
developed based on the AutoCAD .NET API.

The goal is to automatically read drawing information, execute
engineering rules, and provide traceable CAD visual feedback.

Core workflow:

    Read Data → Analyze Rules → Generate Results → CAD Visualization

## Current Version

v1.5.0 Development

## Implemented Features

### Drawing Inspection

Supports:

-   Drawing number consistency checking
-   Title block inspection
-   Revision inspection
-   Basic project number checking

### BOM Inspection

Supports:

-   CAD BOM table reading
-   BOM data parsing
-   Part information extraction

Workflow:

    DWG
     ↓
    CadTableReader
     ↓
    BomTableRecognizer
     ↓
    BomData

### Standard Part Inspection

Supports:

-   StandardParts.xlsx database
-   Standard part cache
-   Part number indexing
-   Loose part number matching
-   Strict format validation
-   Name validation
-   Missing standard detection
-   Multiple match detection
-   NS non-standard filtering

### Marker System

Supports:

-   Error location markers
-   Dedicated marker layer
-   XData association
-   Safe marker removal

## Version Roadmap

v1.4.0

BOM Extraction and Recognition System

v1.5.0

Standard Part Checking System

v1.6.0

Intelligent Correction and Knowledge Base
## 项目简介

CADCheckTool_1 是基于 AutoCAD .NET API 开发的机械工程图智能审核插件。

目标：

自动读取工程图数据，执行工程规则检查，并在 CAD 中生成可追踪的错误标记。

核心流程：

    读取数据 → 分析规则 → 生成结果 → CAD可视化反馈

## 当前版本

v1.5.0 Development

## 已实现功能

### 工程图检查

支持：

-   图号一致性检查
-   标题栏检查
-   修改记录检查
-   项目号检查基础逻辑

### BOM检查

支持：

-   CAD BOM表读取
-   BOM数据解析
-   零件信息提取

流程：

    DWG
     ↓
    CadTableReader
     ↓
    BomTableRecognizer
     ↓
    BomData

### 标准件检查

支持：

-   StandardParts.xlsx标准件库
-   标准件缓存
-   图号索引
-   图号宽松匹配
-   严格格式检查
-   名称检查
-   未收录检测
-   多匹配检测
-   NS非标件过滤

### Marker系统

支持：

-   错误位置标记
-   专用Marker图层
-   XData关联
-   安全清除

## 版本规划

v1.4.0

BOM Extraction and Recognition System

v1.5.0

Standard Part Checking System

v1.6.0

Intelligent Correction and Knowledge Base