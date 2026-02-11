@echo off
echo ========================================
echo   DeviceMonitor - Hardware Monitor
echo ========================================
echo.
echo Starting DeviceMonitor with Administrator privileges...
echo.

REM Check if already running as admin
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with Administrator privileges...
    start "" "%~dp0DeviceMonitor.exe"
) else (
    echo Requesting Administrator privileges...
    powershell -Command "Start-Process '%~dp0DeviceMonitor.exe' -Verb RunAs"
)

echo.
echo If the application doesn't start, please:
echo 1. Make sure .NET 10.0 Runtime is installed
echo 2. Right-click DeviceMonitor.exe and select "Run as administrator"
echo.
pause
