@echo off
setlocal
rem Use Windows PowerShell, regardless of the destination PC's PATH.
set "ScreenInkPowerShell=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if not exist "%ScreenInkPowerShell%" (
    echo ScreenInk needs Windows PowerShell 5.1 on Windows 10 or 11.
    pause
    exit /b 1
)
for %%F in (ScreenInk.ps1 ScreenInk.Core.cs ScreenInk.Studio.cs ScreenInk.Dock.cs ScreenInk.ShortcutTests.cs) do (
    if not exist "%~dp0%%F" (
        echo Missing %%F. Extract or copy the entire ScreenInk folder first.
        pause
        exit /b 1
    )
)
start "ScreenInk" "%ScreenInkPowerShell%" -STA -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0ScreenInk.ps1"
endlocal
