[CmdletBinding()]
param([string]$Version = '1.0.0', [string]$OutputPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $OutputPath) { $OutputPath = Join-Path $root 'dist' }
New-Item -ItemType Directory -Force $OutputPath | Out-Null
$stage = Join-Path $OutputPath 'ScreenInk Portable'
New-Item -ItemType Directory -Force $stage | Out-Null
$files = @('ScreenInk.ps1','ScreenInk.Core.cs','ScreenInk.Studio.cs','ScreenInk.Dock.cs','ScreenInk.ShortcutTests.cs','Start ScreenInk.bat','START HERE.txt','README.md','CONTRIBUTING.md','LICENSE','Whiteboard.html')
foreach ($file in $files) { Copy-Item -LiteralPath (Join-Path $root $file) -Destination $stage -Force }
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination $stage -Recurse -Force
$archive = Join-Path $OutputPath "ScreenInk-v$Version-Windows-Portable.zip"
Compress-Archive -LiteralPath $stage -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath (Join-Path $OutputPath 'SHA256SUMS.txt') -Encoding ASCII
Write-Output $archive
