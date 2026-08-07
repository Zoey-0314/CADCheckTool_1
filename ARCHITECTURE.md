# CADCheckTool_1 Architecture

## Overview

CADCheckTool_1 uses a layered architecture.

Design principle:

Read Data → Analyze Data → Generate Result → Modify CAD

# Project Layers

    Correct_test1

    ├── Readers

    ├── Checks

    ├── Markers

    ├── Batch

    ├── Configs

    ├── Core

    ├── Models

    └── Export

# Readers

Responsibility:

Read information from DWG files.

Examples:

-   Layout information
-   Title block text
-   Revision table content

Readers should not modify CAD entities.

# Checks

Responsibility:

Implement business validation rules.

Examples:

-   Drawing number checking
-   Revision checking

Checks should not directly create CAD graphics.

# Markers

Responsibility:

Create CAD annotations.

Examples:

-   Error rectangles
-   Text labels

Marker classes should inherit from:

    MarkerBase

# Configs

Responsibility:

Store configurable parameters.

Purpose:

Avoid hard-coded engineering rules.

Bad:

``` csharp
double width = 18;
```

Good:

``` csharp
MarkerConfig.RevisionBoxWidth;
```

# Core

Common infrastructure.

Current components:

-   AppLogger
-   SafeDwgSaver

Future components:

-   ExceptionHelper
-   TransactionHelper

# Design Rules

1.  Readers only read data.

2.  Checks only analyze data.

3.  Markers only modify CAD.

4.  Company-specific rules belong in Configs.

5.  Avoid magic numbers.

6.  Keep CAD transaction operations controlled.
