# CADCheckTool_1

AutoCAD engineering drawing automatic inspection plugin.

![AutoCAD](https://img.shields.io/badge/AutoCAD-.NET%20API-blue)
![CSharp](https://img.shields.io/badge/C%23-.NET-purple)
![Version](https://img.shields.io/badge/version-v1.6.5-green)
------------------------------------------------------------------------

## Project Introduction

CADCheckTool_1 is a mechanical engineering drawing inspection plugin
developed based on the AutoCAD .NET API.

The goal is to automatically read drawing information, execute
engineering rules, and provide traceable CAD visual feedback.

Core workflow:

    Read Data → Analyze Rules → Generate Results → CAD Visualization

## Current Version

v1.6.5 Release

## Implemented Features

### Drawing Inspection

Supports:

-   Drawing number consistency checking
-   Title block inspection
-   Revision inspection
-   Basic project number checking
-   Page number checking
-   Technical requirements checking
-   Title block text-height checking

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
-   Dedicated marker layer: `CADCHECK_MARKER`
-   XData association
-   Safe marker removal
-   Text-height error markers
-   Drawing-number marker position adjustment for repeated errors

### Batch Inspection

Supports:

-   Batch drawing inspection
-   Per-drawing result collection
-   Batch marker generation
-   Safe drawing saving

------------------------------------------------------------------------

# v1.6.0 Intelligent Correction and Knowledge Base

## Intelligent Correction System

Added intelligent correction workflow based on inspection results.

Supports:

-   Inspection result classification
-   Error information analysis
-   Correction suggestion generation
-   Correction workflow integration
-   Traceable correction records

Workflow:

    Inspection Result
            ↓
    Error Classification
            ↓
    Correction Suggestion
            ↓
    User Confirmation
            ↓
    CAD Update

## Knowledge Base System

Added engineering knowledge management capability.

Supports:

-   Rule knowledge storage
-   Inspection experience accumulation
-   Correction suggestion association
-   Engineering rule expansion support

------------------------------------------------------------------------

# v1.6.1 Installer Deployment Release

## Installer System

Added complete deployment system for end users.

Supports:

-   Automatic installation package generation
-   Program file deployment
-   Resource file deployment
-   Configuration initialization
-   AutoCAD plugin registration
-   Automatic loading configuration

Installation package:

    CADCheckTool_1_Setup_v1.6.1.exe

Default installation directory:

    C:\Program Files\CADCheckTool_1

Deployment workflow:

    Setup.exe
        ↓
    InstallerLauncher
        ↓
    CADCheckToolInstaller
        ↓
    Registry Registration
        ↓
    AutoCAD Plugin Loading

## Release Files

Release package includes:

    CADCheckTool_1_Setup_v1.6.1.exe

Users only need to run the installer to complete deployment.

------------------------------------------------------------------------

## Version Roadmap

v1.4.0

BOM Extraction and Recognition System

v1.5.0

Standard Part Checking System

v1.6.0

Intelligent Correction and Knowledge Base

v1.6.1

Installer Deployment Release

------------------------------------------------------------------------

# 项目简介

CADCheckTool_1 是基于 AutoCAD .NET API 开发的机械工程图智能审核插件。

目标：

自动读取工程图数据，执行工程规则检查，并在 CAD 中生成可追踪的错误标记。

核心流程：

    读取数据 → 分析规则 → 生成结果 → CAD可视化反馈

## 当前版本

v1.6.5

## 已实现功能

### 工程图检查

支持：

-   图号一致性检查
-   标题栏图号与文件名图号比较
-   标题栏图号与BOM表上方图号比较
-   标题栏检查
-   修改记录检查
-   项目号检查基础逻辑
-   页码检查
-   技术要求检查
-   标题栏文字高度检查

### BOM检查

支持：

-   CAD BOM表读取
-   BOM数据解析
-   零件信息提取
-   BOM表头附近图号识别
-   BOM图号参与图号一致性检查

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
-   统一Marker图层：`CADCHECK_MARKER`
-   XData关联
-   安全清除
-   字高错误Marker
-   同一布局图号区域重复Marker位置调整

### 批量检查

支持：

-   多张DWG批量检查
-   批量结果汇总
-   批量Marker生成
-   安全保存图纸

------------------------------------------------------------------------

# v1.6.0 智能修正与知识库

新增：

-   审核结果分类
-   错误信息分析
-   修正建议生成
-   修正流程管理
-   修正记录追踪
-   工程知识库存储

------------------------------------------------------------------------

# v1.6.1 安装部署版本

新增完整软件部署体系。

支持：

-   自动生成安装包
-   程序文件部署
-   资源文件部署
-   配置初始化
-   AutoCAD插件注册
-   自动加载配置

安装包：

    CADCheckTool_1_Setup_v1.6.1.exe

默认安装目录：

    C:\Program Files\CADCheckTool_1

部署流程：

    安装程序
        ↓
    InstallerLauncher
        ↓
    CADCheckToolInstaller
        ↓
    注册表注册
        ↓
    AutoCAD插件加载

Release包含：

    CADCheckTool_1_Setup_v1.6.1.exe

------------------------------------------------------------------------

## 版本规划

v1.4.0

BOM Extraction and Recognition System

v1.5.0

Standard Part Checking System

v1.6.0

Intelligent Correction and Knowledge Base

v1.6.1

Installer Deployment Release
