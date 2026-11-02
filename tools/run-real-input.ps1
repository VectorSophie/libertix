# Compile RealInput.cs into an existing renderer copy and run it once with a hard stop.
# Usage: run-real-input.ps1 <renderer copy dir> <output prefix>
param([string]$Dir, [string]$Out)
$ErrorActionPreference = 'Stop'
$wpf = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:"$Dir\RealInput.exe" `
    /win32manifest:"$PSScriptRoot\RealInput.manifest" /r:"$Dir\Libertix.exe" /r:"$wpf\PresentationFramework.dll" `
    /r:"$wpf\PresentationCore.dll" /r:"$wpf\WindowsBase.dll" /r:"$wpf\System.Xaml.dll" "$PSScriptRoot\RealInput.cs"
if ($LASTEXITCODE -ne 0) { throw "csc failed" }
Copy-Item "$Dir\Libertix.exe.config" "$Dir\RealInput.exe.config"
$p = Start-Process "$Dir\RealInput.exe" -ArgumentList $Out -WorkingDirectory $Dir -PassThru
if (-not $p.WaitForExit(90000)) { Stop-Process -Id $p.Id -Force; "STOPPED" } else { "exit=$($p.ExitCode)" }
Get-Content "$Out.txt"
