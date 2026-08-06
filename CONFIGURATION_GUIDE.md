# Configuration Guide

## Purpose

CADCheckTool_1 is designed for engineering drawing inspection.

However, engineering standards differ between companies.

Some rules must be customized before deployment.

# 1. Drawing Number Rules

## Description

The system checks whether the drawing number is consistent between:

-   DWG file information
-   Title block information

Example:

File:

    ABC-001.dwg

Title block:

    ABC-002

Result:

Drawing number mismatch.

## Customization Location

Related code:

    Checks/
    Readers/
    Configs/

Possible changes:

-   Prefix format
-   Number length
-   Separator
-   Revision suffix

Example:

Company A:

    ABC-001

Company B:

    PRJ_2026_001

The validation rule should be modified according to company
requirements.

# 2. Project Number Rules

Project number formats are usually company-specific.

Possible storage locations:

-   Title block
-   File name
-   Drawing properties
-   Custom attributes

Customization locations:

    Checks/
    Models/
    Configs/

Possible parameters:

-   Prefix
-   Length
-   Regular expression
-   Position

# 3. Title Block Configuration

Title block layouts vary between templates.

Configuration files:

    Configs/

    TitleBlockConfig.cs
    TitleBlockHorizontalConfig.cs
    TitleBlockVerticalConfig.cs

Modify when:

-   Template changes
-   Title block moves
-   Text position changes

# 4. Revision Configuration

Revision related parameters are stored in Configs.

Possible customization:

-   Detection area
-   Marker size
-   Layer name
-   Tolerance

# 5. Marker Configuration

Marker appearance is controlled by:

    Configs/
    MarkerConfig.cs

Examples:

-   Layer names
-   Rectangle width
-   Rectangle height
-   Text size

# Code Maintenance Rule

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
