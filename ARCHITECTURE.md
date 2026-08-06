# Correct_test1 Architecture Documentation

## 1. Overview

Correct_test1 is an AutoCAD .NET API based drawing quality inspection plugin.

The project is designed to automatically inspect engineering drawings, analyze drawing information, identify potential issues, generate visual inspection markers, and support batch processing of multiple DWG files.

The system follows a layered architecture to separate:

- DWG data reading
- Inspection logic
- Drawing modification
- Batch processing
- Export and reporting

The main design goal is:

> Keep data reading, business checking, and DWG modification independent, so that the system remains maintainable and extensible.

---

# 2. System Architecture

The overall architecture:

```
                 AutoCAD Database
                        |
                        |
                  Batch Layer
                        |
        +---------------+---------------+
        |                               |
   Readers Layer                  Checks Layer
        |                               |
        |                               |
  Extract DWG data              Analyze rules
        |                               |
        +---------------+---------------+
                        |
                    Models
                        |
                        |
                  Markers Layer
                        |
                        |
              Modify DWG Database
                        |
                        |
                  Export Layer
```


---

# 3. Project Structure

```
Correct_test1

├── Batch
│   ├── BatchCheckerManager.cs
│   └── BatchMarkerCleaner.cs
│
├── Checks
│   ├── TitleBlockCheckManager.cs
│   └── RevisionChecker.cs
│
├── Core
│   ├── AppLogger.cs
│   └── SafeDwgSaver.cs
│
├── Export
│
├── Markers
│   ├── MarkerBase.cs
│   ├── RevisionMarker.cs
│   ├── TitleBlockDrawingNumberMarker.cs
│   └── ErrorMarker.cs
│
├── Models
│
├── Readers
│   ├── LayoutReader.cs
│   ├── TitleBlockReader.cs
│   └── RevisionTableReader.cs
│
└── Command
```

---

# 4. Layer Responsibilities

## 4.1 Readers Layer

### Responsibility

Readers are responsible for extracting information from AutoCAD Database.

Readers only read data.

They should not:

- modify entities
- create layers
- draw markers
- save DWG files


Example:

```
LayoutReader
        |
        |
        v

LayoutInfo
```


Typical operations:

- Read Layout information
- Read title block text
- Read revision table content
- Extract entity properties


Design rule:

```
Readers -> Database Read Only
```

---

# 4.2 Models Layer

### Responsibility

Models are pure data containers.

Models represent information exchanged between modules.


Examples:

```
LayoutInfo

RevisionInfo

TitleText

RevisionCheckIssue

CheckResult
```


Models should:

- contain properties
- contain simple data structures


Models should NOT:

- access AutoCAD API
- perform checks
- modify drawings


Design rule:

```
Models contain data only.
```

---

# 4.3 Checks Layer

### Responsibility

Checks contain business inspection rules.


Examples:

```
TitleBlockCheckManager

RevisionChecker
```


Checks analyze data from Readers.

Flow:

```
Readers

↓

Models

↓

Checks

↓

CheckResult / Issue List
```


Checks should NOT:

- create AutoCAD entities
- modify DWG
- directly save files


Design rule:

```
Checks decide what is wrong.
Markers decide how to show it.
```

---

# 4.4 Markers Layer

### Responsibility

Markers are responsible for visual feedback inside DWG.


Examples:

```
RevisionMarker

TitleBlockDrawingNumberMarker

ErrorMarker
```


Markers can:

- create layers
- create entities
- modify Database


Markers must follow AutoCAD API rules:

1. Use Transaction

2. Use LayerId instead of string layer assignment

3. Call SetDatabaseDefaults()

4. AppendEntity before AddNewlyCreatedDBObject()

5. Commit transaction properly


Standard flow:

```
Start Transaction

↓

Ensure Layer

↓

Create Entity

↓

Set Database Defaults

↓

Append Entity

↓

Register Entity

↓

Commit

```

---

# 4.5 Batch Layer

### Responsibility

Batch layer manages multiple DWG files.


Main functions:

- Load DWG
- Execute checking process
- Save results
- Release Database resources


Flow:

```
DWG Files

↓

Create Database

↓

Read DWG

↓

DrawingCheckManager

↓

SafeDwgSaver

↓

Dispose Database
```


Batch layer must:

- isolate file errors
- continue processing remaining files
- never damage original drawings


---

# 4.6 Core Layer

Core contains common infrastructure.


## AppLogger

Purpose:

Unified logging system.


Rules:

Never use:

```
Debug.WriteLine()

File.AppendAllText()

```

Use:

```
AppLogger.Info()

AppLogger.Warn()

AppLogger.Error()

```


Log location:

```
%AppData%\Correct_test1\Logs
```


---

## SafeDwgSaver

Purpose:

Prevent DWG corruption caused by direct overwrite.


Never use:

```
Database.SaveAs(originalFile)
```


Use:

```
SafeDwgSaver.Save()
```


Saving process:

```
Database

↓

Temporary DWG

↓

Validation

↓

Replace original file

```

---

# 5. Data Flow

Complete inspection workflow:


```
User Command

        |

        v

BatchCheckerManager

        |

        v

Database.Load DWG

        |

        v

Readers

        |

        v

Models

        |

        v

Checks

        |

        v

Issue Results

        |

        v

Markers

        |

        v

SafeDwgSaver

        |

        v

Output DWG
```


---

# 6. AutoCAD API Design Rules


## Transaction Management

All Database operations must be inside:

```csharp
using(Transaction tr =
    db.TransactionManager.StartTransaction())
{

}
```


Never keep:

- Transaction
- Entity reference
- Open database object

outside its lifetime.


---

## Entity Creation Rules


Correct:

```csharp
entity.SetDatabaseDefaults(db);

btr.AppendEntity(entity);

tr.AddNewlyCreatedDBObject(entity,true);
```


Incorrect:

```csharp
btr.AppendEntity(entity);
```

without registration.


---

## Layer Management


All Marker classes should inherit:

```
MarkerBase
```


Layer creation should use:

```
EnsureLayer()
```


Do not duplicate layer creation logic.


---

# 7. Error Handling Strategy


Errors are divided into three levels.


## Recoverable Error

Example:

- Missing title text
- Missing optional information


Action:

Continue checking.

---

## File Level Error

Example:

- DWG cannot read
- Save failed


Action:

Record error and skip current file.


---

## Critical Error

Example:

- Database corruption risk
- AutoCAD API crash


Action:

Stop current operation and preserve original file.


---

# 8. Extension Guidelines


When adding new inspection features:


## Step 1

Create Reader.

Example:

```
NewTableReader
```


Only extract data.


---

## Step 2

Create Model.


Example:

```
NewTableInfo
```


Store information.


---

## Step 3

Create Check.


Example:

```
NewTableChecker
```


Implement rules.


---

## Step 4

Create Marker.


Example:

```
NewTableMarker
```


Display issues.


---

# 9. Future Improvement Plan


## v1.4

Engineering improvements:

- Unified Transaction Helper
- Configuration system
- Exception handling framework
- Remove magic numbers


## v1.5

Inspection improvements:

- More drawing rules
- Template configuration
- Advanced reports


## v2.0

Productization:

- User interface
- Configuration management
- Plugin deployment system


---

# 10. Development Philosophy


The project follows these principles:


## Separation of Responsibility

Each module should do one thing.


## Safety First

Never risk damaging original DWG files.


## Explicit Data Flow

Data should move:

```
Reader

↓

Model

↓

Check

↓

Marker

```

not bypass layers.


## Maintainability Over Quick Fixes

Temporary debugging solutions must eventually be replaced by standardized components.
