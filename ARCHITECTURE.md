# CADCheckTool_1 Architecture

## Overview

CADCheckTool_1 uses a layered architecture.

## Design Principle

    Read Data → Analyze Data → Generate Result → Modify CAD

## Project Structure

    Correct_test1

    ├── Command
    ├── Readers
    ├── Checks
    ├── Markers
    ├── Batch
    ├── Core
    ├── Models
    ├── Configs
    └── Export

## Module Responsibilities

### Readers

Responsible for:

-   DWG data reading
-   BOM reading
-   Title block reading
-   Revision reading

### Checks

Responsible for:

-   Drawing number checking
-   Revision checking
-   Project number checking
-   BOM validation
-   Standard part verification

### Markers

Responsible for:

-   CAD error visualization
-   Marker lifecycle management
-   XData association
-   Safe removal

### Core

Responsible for:

-   Logging
-   Safe saving
-   Common services

## Architecture Rules

Forbidden:

-   Readers modifying CAD
-   Checks creating CAD entities directly
-   Markers executing business rules
---
## 设计原则

    读取数据 → 分析数据 → 生成结果 → 修改CAD

## 项目结构

    Correct_test1

    ├── Command
    ├── Readers
    ├── Checks
    ├── Markers
    ├── Batch
    ├── Core
    ├── Models
    ├── Configs
    └── Export

## 模块职责

### Readers

负责：

-   DWG数据读取
-   BOM读取
-   标题栏读取
-   修改记录读取

### Checks

负责：

-   图号检查
-   修改记录检查
-   项目号检查
-   BOM检查
-   标准件检查

### Markers

负责：

-   CAD错误显示
-   标记生命周期管理
-   XData绑定
-   安全清除

### Core

负责：

-   日志系统
-   安全保存
-   公共服务

## 架构规则

禁止：

-   Reader修改CAD
-   Check直接创建CAD实体
-   Marker执行业务判断
