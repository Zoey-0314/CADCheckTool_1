$ErrorActionPreference = 'Stop'

$installerDir = $PSScriptRoot
$packageRoot = Split-Path -Parent $installerDir
$bundleRoot = Join-Path $packageRoot 'artifacts\bundle\CADCheckTool.bundle'
$pluginDir = Join-Path $bundleRoot 'Contents\Windows'
$issPath = Join-Path $installerDir 'CADCheckTool_v2.2.0.iss'
$outputPath = Join-Path $packageRoot 'artifacts\release\CADCheckTool_1_Setup_v2.2.0.exe'

$requiredFiles = @(
  'CADCheckTool_1.dll',
  'CADCheckTool_1.dll.config',
  'EPPlus.dll',
  'Microsoft.IO.RecyclableMemoryStream.dll',
  'System.Buffers.dll',
  'System.ComponentModel.Annotations.dll',
  'System.Memory.dll',
  'System.Numerics.Vectors.dll',
  'System.Runtime.CompilerServices.Unsafe.dll',
  'System.Security.Cryptography.Xml.dll'
)

if (-not (Test-Path $issPath -PathType Leaf)) {
  throw "Inno Setup source file is missing: $issPath"
}

if (-not (Test-Path (Join-Path $bundleRoot 'PackageContents.xml') -PathType Leaf)) {
  throw "AutoCAD bundle manifest is missing: $bundleRoot\PackageContents.xml"
}

$missingFiles = @(
  foreach ($file in $requiredFiles) {
    if (-not (Test-Path (Join-Path $pluginDir $file) -PathType Leaf)) {
      $file
    }
  }
)

if ($missingFiles.Count -gt 0) {
  throw ('Required runtime files are missing:' + [Environment]::NewLine + ($missingFiles -join [Environment]::NewLine))
}

$recyclableStreamPath = Join-Path $pluginDir 'Microsoft.IO.RecyclableMemoryStream.dll'
$recyclableStreamVersion =
  [Reflection.AssemblyName]::GetAssemblyName($recyclableStreamPath).Version.ToString()
if ($recyclableStreamVersion -ne '1.4.1.0') {
  throw "Microsoft.IO.RecyclableMemoryStream.dll must have assembly version 1.4.1.0 for EPPlus 5.8.0. Actual: $recyclableStreamVersion"
}

$isccCandidates = @(
  (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
  (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
) | Where-Object { $_ -and (Test-Path $_ -PathType Leaf) }

$iscc = $isccCandidates | Select-Object -First 1
if (-not $iscc) {
  throw 'Inno Setup 6 was not found. Install Inno Setup 6, then run this file again.'
}

Write-Host 'Runtime dependency validation passed.' -ForegroundColor Green
Write-Host 'Compiling CADCheckTool installer...'
& $iscc $issPath
if ($LASTEXITCODE -ne 0) {
  throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $outputPath -PathType Leaf)) {
  throw "Compilation finished, but the expected installer was not found: $outputPath"
}

Write-Host ''
Write-Host 'Installer created successfully:' -ForegroundColor Green
Write-Host $outputPath
