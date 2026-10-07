# Deterministic replay of real canvas mouse handlers. No desktop capture.
[CmdletBinding()]
param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
if (-not $OutputPath) { $OutputPath = Join-Path $PSScriptRoot '..\demo-replay-frames' }
$root = Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
Add-Type -Path @("$root\ScreenInk.Core.cs","$root\ScreenInk.Studio.cs","$root\ScreenInk.Dock.cs","$PSScriptRoot\DemoReplay.cs") -ReferencedAssemblies System.Windows.Forms.dll,System.Drawing.dll
[System.Windows.Forms.Application]::EnableVisualStyles()
[DemoReplay]::Run([IO.Path]::GetFullPath($OutputPath))
