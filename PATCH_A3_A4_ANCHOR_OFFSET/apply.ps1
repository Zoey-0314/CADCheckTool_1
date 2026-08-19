$ErrorActionPreference = 'Stop'

# Windows PowerShell 5.1 treats UTF-8 scripts without BOM as the system ANSI code page.
# The original patch contains Chinese source strings, so parsing it directly can fail.
# This bootstrap is intentionally ASCII-only. It loads the original patch from Git
# as UTF-8 text, then parses and executes the Unicode string in memory.

$sourceCommit = '3a6616db90d775ec172a0d9d206f9a4aec395d4c'
$sourcePath = 'PATCH_A3_A4_ANCHOR_OFFSET/apply.ps1'
$gitSpec = $sourceCommit + ':' + $sourcePath

$repo = (& git rev-parse --show-toplevel 2>$null)
if ([string]::IsNullOrWhiteSpace($repo)) {
    throw 'Run this script inside the CADCheckTool Git repository.'
}

Set-Location $repo

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = 'git.exe'
$psi.Arguments = 'show ' + $gitSpec
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
$psi.StandardOutputEncoding = New-Object System.Text.UTF8Encoding($false)
$psi.StandardErrorEncoding = New-Object System.Text.UTF8Encoding($false)

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $psi

try {
    [void]$process.Start()

    $patchText = $process.StandardOutput.ReadToEnd()
    $gitError = $process.StandardError.ReadToEnd()

    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        throw ('Unable to load patch payload from Git. ' + $gitError)
    }

    if ([string]::IsNullOrWhiteSpace($patchText)) {
        throw 'Patch payload is empty.'
    }

    $scriptBlock = [ScriptBlock]::Create($patchText)
    & $scriptBlock
}
finally {
    $process.Dispose()
}
