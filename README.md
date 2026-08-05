# Correct_test1 - AutoCAD Drawing Quality Inspection Tool

# Correct_test1 - AutoCAD 图纸质量检查工具


![AutoCAD](https://img.shields.io/badge/AutoCAD-.NET%20API-blue)
![C#](https://img.shields.io/badge/C%23-.NET-purple)
![Version](https://img.shields.io/badge/version-v1.2.0-green)


---

# 中文介绍 | Introduction (Chinese)

## 项目简介

Correct_test1 是一个基于 **AutoCAD .NET API** 开发的 CAD 图纸自动检查插件。

该项目旨在帮助工程设计人员自动检查 DWG 图纸中的标准化问题，
减少人工检查工作量，提高图纸审核效率。

当前版本主要实现：

- 修改记录自动检查
- 标题栏信息读取与检查
- 图号一致性检查
- 自动生成 CAD 内绿色错误标记
- 批量 DWG 检查
- CSV 检查报告导出


---

# English Introduction

## Project Overview

Correct_test1 is an AutoCAD plugin developed with **AutoCAD .NET API**.

The purpose of this project is to automatically inspect engineering drawings,
reduce manual checking workload, and improve CAD drawing quality control efficiency.

Current features include:

- Revision table inspection
- Title block extraction and validation
- Drawing number consistency checking
- Automatic CAD error marking
- Batch DWG inspection
- CSV report exporting


---

# 技术栈 | Technology Stack

## Development Environment

| 项目 | 技术 |
|---|---|
| Language | C# |
| Platform | AutoCAD .NET API |
| Framework | .NET Framework |
| CAD Format | DWG |
| IDE | Visual Studio |
| Version Control | Git |


---

# 项目结构 | Project Structure


Correct_test1
│
├── Commands
│ └── AutoCAD Commands Entry
│
├── Models
│ ├── DrawingInfo
│ ├── LayoutInfo
│ ├── CheckResult
│ └── RevisionInfo
│
├── Readers
│ ├── LayoutReader
│ ├── TitleBlockReader
│ ├── RevisionTableReader
│ └── FileNameDrawingNumberReader
│
├── Parsers
│ └── TitleBlockRegionParser
│
├── Checks
│ ├── TitleBlockCheckManager
│ ├── TitleBlockChecker
│ ├── RevisionChecker
│ └── DrawingCheckManager
│
├── Markers
│ ├── RevisionMarker
│ └── TitleBlockDrawingNumberMarker
│
├── Batch
│ ├── BatchCheckerManager
│ └── BatchMarkerCleaner
│
└── Export
└── BatchCsvExporter


---

# 已实现功能 | Features


## 1. 修改记录检查
## Revision Table Inspection

支持自动读取 CAD 修改记录表：

- 标记
- 修改内容
- 日期
- 签名


自动判断：

- 缺少日期
- 缺少修改内容
- 缺少签名


并生成：

- CheckResult
- CAD绿色标记


---

## 2. 标题栏解析
## Title Block Extraction

支持读取标题栏信息：

### 基础信息

- 图号 Drawing Number
- 图纸名称 Drawing Name
- 公司 Company
- 材料 Material
- 规格 Specification
- 表面处理 Surface Treatment


### 签字信息

- 制图 Designer
- 校对 Checker
- 标审 Reviewer
- 批准 Approver
- 日期 Date


---

## 3. 横版 / 竖版自动识别
## Landscape / Portrait Detection


通过标题栏布局内部文字特征判断：


标记数量 >= 2
|
↓
Horizontal

标记数量 < 2
|
↓
Vertical



判断以 Layout 为单位，
避免多个布局之间相互影响。


---

## 4. 图号一致性检查
## Drawing Number Validation


自动比较：


DWG文件名图号

    VS

标题栏图号



例如：

文件：


NS135H xxxx.dwg



标题栏：


NS136H



系统自动识别错误。


---

## 5. CAD自动错误标记
## Automatic CAD Marking


错误位置自动生成：

### 图层


TITLEBLOCK_CHECK



生成：

- 绿色矩形框
- 修改提示文字


例如：


应改为:NS135H



---

## 6. 批量检查
## Batch Inspection


支持：


文件夹
|
├── Drawing1.dwg
├── Drawing2.dwg
└── Drawing3.dwg

    ↓

自动检查

    ↓

CSV报告



---

## 7. CSV报告导出
## CSV Report Export


生成：


批量检查结果_xxxxxx.csv



包含：

|字段|说明|
|-|-|
|文件名|DWG文件|
|打开图纸|快捷链接|
|布局|Layout|
|检查类型|Issue Type|
|缺失项|Missing Field|
|问题描述|Description|


---

# 版本记录 | Version History


## v1.2.0

### Title Block Validation Release

新增：

- 标题栏自动读取
- 标题栏字段检查
- 文件名图号检查
- 图号错误绿色标记
- 批量模式 Marker 支持


---

## v1.1.1

### Code Maintenance Release

- Code formatting improvement
- Structure optimization
- No functional changes


---

# 开发计划 | Roadmap


## v1.3.0

计划：

- BOM表读取
- 零件 / 组件自动识别
- 装配图检查


---

## v1.4.0

计划：

- 更复杂材料规格规则
- 页码自动检查
- 更多企业标准支持


---

# 使用流程 | Workflow



打开AutoCAD

    ↓

加载插件 DLL

    ↓

选择DWG文件

    ↓

读取布局

    ↓

解析标题栏

    ↓

执行检查

    ↓

生成错误标记

    ↓

导出检查报告



---

# License

This project is currently for learning and engineering automation research.

本项目目前用于 CAD 自动化学习与工程应用研究。


---

# Author

Developed with C# and AutoCAD .NET API.

基于 C# 与 AutoCAD .NET API 开发。