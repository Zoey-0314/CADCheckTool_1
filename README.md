# Correct_test1

## AutoCAD Drawing Quality Inspection Tool

![AutoCAD](https://img.shields.io/badge/AutoCAD-.NET%20API-blue)
![CSharp](https://img.shields.io/badge/C%23-.NET-purple)
![Version](https://img.shields.io/badge/version-v1.2.0-green)

---

# English

## Overview

Correct_test1 is an AutoCAD plugin developed with **C# and AutoCAD .NET API** for automated engineering drawing quality inspection.

The project aims to improve the efficiency and accuracy of CAD drawing review by automatically extracting drawing information, checking engineering standards, generating visual error markers, and exporting inspection reports.

The current version supports DWG drawing inspection, including revision table checking, title block validation, drawing number consistency checking, CAD annotation generation, and batch processing.

---

## Features

### Drawing Information Extraction

The plugin can automatically read information from AutoCAD drawings, including:

- Layout information
- Title block contents
- Revision table contents
- Drawing number information


### Title Block Inspection

The system analyzes title block information and checks:

- Missing required fields
- Drawing number consistency
- Designer information
- Checker information
- Approval information
- Date information


### Drawing Number Validation

The plugin compares the drawing number extracted from the DWG file name with the drawing number inside the title block.

Example:

```
DWG File Name:

NS135H_xxxxx.dwg


Title Block:

NS136H
```

When an inconsistency is detected, the system automatically generates a warning marker inside AutoCAD.


### CAD Visual Marking

The plugin creates a dedicated checking layer:

```
TITLEBLOCK_CHECK
```

and generates:

- Green error frames
- Correction notes

Example:

```
应改为:NS135H
```


### Revision Table Inspection

The system supports automatic revision table checking, including:

- Revision mark
- Revision description
- Date
- Signature information

The inspection results are converted into structured check results for further processing.


### Batch DWG Inspection

The system supports batch inspection of multiple DWG files.

Workflow:

```
DWG Files

    ↓

Automatic Inspection

    ↓

Error Detection

    ↓

CAD Marking

    ↓

CSV Report Export
```


### Report Export

Inspection results can be exported as CSV files.

The report contains:

- File name
- Drawing path
- Layout name
- Inspection type
- Missing item
- Error description
- Expected value


---

## Architecture

The project adopts a modular layered architecture.

```
Correct_test1

├── Command
│   └── AutoCAD command entry

├── Readers
│   ├── Layout Reader
│   ├── Title Block Reader
│   ├── Revision Table Reader
│   └── File Name Reader

├── Parsers
│   └── CAD text parsing and conversion

├── Checks
│   ├── Title Block Check
│   ├── Revision Check
│   └── Drawing Check Management

├── Markers
│   ├── Revision Marker
│   └── Title Block Drawing Number Marker

├── Batch
│   ├── Batch Checker
│   └── Marker Cleaner

├── Export
│   └── CSV Export

└── Models
    └── Data Models
```


---

## Technology Stack

| Component | Technology |
| --- | --- |
| Programming Language | C# |
| CAD Platform | AutoCAD .NET API |
| Framework | .NET Framework |
| File Format | DWG |
| Development Environment | Visual Studio |
| Version Control | Git |


---

## Installation

### Requirements

- AutoCAD
- Visual Studio
- .NET Framework


### Steps

Clone the repository:

```bash
git clone https://github.com/Zoey-0314/Correct_test1.git
```

Open the solution:

```
Correct_test1.sln
```

Build the project and load the generated DLL into AutoCAD.


---

## Usage

1. Open an AutoCAD drawing.

2. Load the plugin DLL.

3. Run the inspection command.

4. The system will:

- Read drawing information
- Analyze title blocks
- Check revision tables
- Generate CAD error markers
- Export inspection reports


---

## Roadmap

### v1.3.0

Planned:

- BOM table extraction
- Part and assembly recognition
- Assembly drawing inspection


### v1.4.0

Planned:

- Advanced material and specification checking
- Page number validation
- Enterprise drawing standard support


---

## Version History

### v1.2.0

Title Block Validation Release

Added:

- Title block information extraction
- Title block field inspection
- Drawing number consistency checking
- Automatic CAD error marking
- Batch inspection support


### v1.1.1

Maintenance Release

- Code structure optimization
- Formatting improvements
- No functional changes


---

# 中文

## 项目简介

Correct_test1 是一个基于 **C# 和 AutoCAD .NET API** 开发的工程图纸自动检查插件。

项目目标是通过自动读取 DWG 图纸信息、执行标准化检查、生成 CAD 内部错误标记以及导出检查报告，提高机械工程图纸审核效率，减少人工检查工作量。

当前版本主要支持：

- 修改记录检查
- 标题栏检查
- 图号一致性检查
- CAD 自动错误标记
- 批量 DWG 检查
- CSV 检查报告导出


---

## 功能介绍

### 图纸信息读取

系统可以自动读取 DWG 文件中的：

- 布局信息
- 标题栏信息
- 修改记录表信息
- 文件名图号信息


### 标题栏检查

系统自动分析标题栏内容，并检查：

- 必填字段缺失
- 图号错误
- 制图信息
- 校对信息
- 审核信息
- 日期信息


### 图号一致性检查

系统自动比较：

```
DWG文件名图号

        与

标题栏图号
```

例如：

```
文件名:

NS135H_xxxxx.dwg


标题栏:

NS136H
```

发现错误后，自动生成 CAD 内部提示。


### CAD错误标记

系统创建专用检查图层：

```
TITLEBLOCK_CHECK
```

并生成：

- 绿色错误框
- 修改提示文字


例如：

```
应改为:NS135H
```


### 修改记录检查

支持自动读取修改记录表：

- 修改标记
- 修改内容
- 日期
- 签名


### 批量检查

支持对文件夹内多个 DWG 文件进行自动检查。

流程：

```
DWG文件

 ↓

自动读取

 ↓

规则检查

 ↓

错误标记

 ↓

CSV报告
```


### 检查结果导出

系统支持生成 CSV 检查报告，包括：

- 文件名
- 文件路径
- 布局名称
- 检查类型
- 缺失项
- 错误描述
- 正确值


---

## 项目结构

项目采用模块化分层设计：

```
Correct_test1

├── Command
│   └── AutoCAD命令入口

├── Readers
│   └── 数据读取模块

├── Parsers
│   └── 数据解析模块

├── Checks
│   └── 检查规则模块

├── Markers
│   └── CAD标记生成模块

├── Batch
│   └── 批量处理模块

├── Export
│   └── 数据导出模块

└── Models
    └── 数据模型
```


---

## 技术栈

| 项目 | 技术 |
| --- | --- |
| 开发语言 | C# |
| CAD平台 | AutoCAD .NET API |
| 文件格式 | DWG |
| 开发环境 | Visual Studio |
| 版本管理 | Git |


---

## 后续计划

### v1.3.0

计划增加：

- BOM表读取
- 零件与组件识别
- 装配图检查


### v1.4.0

计划增加：

- 材料规格复杂规则检查
- 页码自动检查
- 企业标准支持


---

## 当前版本

```
v1.2.0
```

---

## License

This project is currently used for CAD automation research and engineering application development.

本项目目前用于 CAD 自动化研究与工程应用开发。