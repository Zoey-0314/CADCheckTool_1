# CADCheckTool v2.2.0

CADCheckTool v2.2.0 is the delivery build for AutoCAD 2024 and Windows x64.

## Installation

1. Download `CADCheckTool_1_v2.2.0_Windows_x64.zip`.
2. Extract the ZIP.
3. Close AutoCAD completely.
4. Run `CADCheckTool_1_Setup_v2.2.0.exe`.
5. Start AutoCAD 2024 and run `CHECKDRAWING`.

The installer deploys a complete AutoCAD application bundle to the trusted `C:\Program Files\Autodesk\ApplicationPlugins` directory. Windows requests administrator approval once during installation. No manual `NETLOAD` or AutoCAD registry edits are required, and legacy CADCheckTool registry entries are removed automatically to prevent duplicate loading.

## Package contents

- Self-contained Inno Setup installer
- CADCheckTool plugin assembly
- EPPlus and all other third-party runtime dependencies
- AutoCAD `PackageContents.xml`
- Installation guide
- SHA-256 checksums

AutoCAD's own managed API assemblies are intentionally excluded because they are supplied by AutoCAD 2024.

## Main changes since v2.0.0

- Configurable external data paths
- Current and batch project/version writing
- Drawing version archive comparison for DWG and PDF files
- Non-standard part-number validation
- Cross-layout marker fixes
- Welding-symbol filtering improvements for BOM callouts
- Improved version-check marker placement and text size
- Installer redesigned as a per-user AutoCAD application bundle
