# Copy a build output and compile the offscreen page renderer into it.
# Usage: build-renderer.ps1 <bin\Release dir> <destination dir>
param([string]$Build, [string]$Destination)
$ErrorActionPreference = 'Stop'
if (Test-Path $Destination) { Remove-Item -Recurse -Force -Confirm:$false $Destination }
Copy-Item -Recurse $Build $Destination
$wpf = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:exe /out:"$Destination\RenderPage.exe" `
    /r:"$Destination\Libertix.exe" /r:"$wpf\PresentationFramework.dll" /r:"$wpf\PresentationCore.dll" `
    /r:"$wpf\WindowsBase.dll" /r:"$wpf\System.Xaml.dll" `
    "$PSScriptRoot\RenderPage.cs"
if ($LASTEXITCODE -ne 0) { throw "csc failed" }
# Libertix.exe.config carries the binding redirects its dependencies need.
Copy-Item "$Destination\Libertix.exe.config" "$Destination\RenderPage.exe.config"
