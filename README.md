# CADCheckTool_1

AutoCAD engineering drawing automatic inspection plugin.


![AutoCAD](https://img.shields.io/badge/AutoCAD-.NET%20API-blue)
![CSharp](https://img.shields.io/badge/C%23-.NET-purple)
![Version](https://img.shields.io/badge/version-v1.2.0-green)

## Overview

CADCheckTool_1 is a C# based AutoCAD automation tool used for
engineering drawing inspection.

Main functions:

-   Drawing number consistency checking
-   Title block inspection
-   Revision table inspection
-   Automatic CAD error marking
-   Batch DWG processing
-   Safe DWG saving

Current version:

v1.3.1

# Features

## Drawing Number Validation

The system compares drawing information from different sources:

-   DWG file name
-   Title block drawing number

When inconsistent information is detected:

1.  A check result is generated.
2.  A marker is created in the drawing.
3.  The issue is recorded in the log system.

## Revision Table Inspection

The system reads revision table information and checks:

-   Revision number
-   Revision description
-   Revision location

## Batch Processing

The system supports processing multiple DWG files.

Workflow:

DWG Folder → Read Drawing → Run Checks → Generate Markers → Save Result

Each drawing is processed independently.

# Project Structure

    Correct_test1

    ├── Batch
    │   Batch processing logic

    ├── Checks
    │   Business checking rules

    ├── Configs
    │   Configurable engineering parameters

    ├── Core
    │   Common infrastructure

    ├── Markers
    │   CAD annotation generation

    ├── Models
    │   Data models

    ├── Readers
    │   DWG information extraction

    └── Export
        Result output

# Important Customization Notice

This software contains engineering standard related rules.

Different companies may have different CAD standards.

Before deployment, review:

-   Drawing number rules
-   Project number rules
-   Title block position
-   Revision table format
-   Marker appearance

Detailed configuration information:

See:

CONFIGURATION_GUIDE.md

# Development Documentation

Architecture:

ARCHITECTURE.md

Development workflow:

DEVELOPMENT_WORKFLOW.md
