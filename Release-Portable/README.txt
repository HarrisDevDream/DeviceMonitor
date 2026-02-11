================================================================================
  DeviceMonitor - Hardware Monitoring Application (Portable Version)
================================================================================

Version: 1.0.0
Date: 2026-02-12

DESCRIPTION:
------------
DeviceMonitor is a Windows application that monitors your computer's hardware
sensors including CPU temperature, GPU temperature, fan speeds, and more.

SYSTEM REQUIREMENTS:
--------------------
- Windows 10/11 (64-bit)
- .NET 10.0 Runtime (Download: https://dotnet.microsoft.com/download/dotnet/10.0)
- Administrator privileges (required for hardware access)

HOW TO RUN:
-----------
1. Ensure .NET 10.0 Runtime is installed
2. Right-click DeviceMonitor.exe
3. Select "Run as administrator"
4. Click the "Start Monitoring" button in the application

FEATURES:
---------
✓ Real-time CPU temperature and load monitoring
✓ GPU temperature and load monitoring
✓ Motherboard sensor monitoring
✓ Fan speed monitoring
✓ Automatic logging to AppData\Local\DeviceMonitor\Logs

IMPORTANT NOTES:
----------------
- MUST run as Administrator to access hardware sensors
- First launch may take 5-10 seconds to initialize hardware monitoring
- If monitoring fails, check the log files in:
  %LOCALAPPDATA%\DeviceMonitor\Logs\

TROUBLESHOOTING:
----------------
Q: Application won't start?
A: Make sure you have .NET 10.0 Runtime installed

Q: "Failed to start hardware monitoring" error?
A: Ensure you're running the application as Administrator

Q: No sensors showing?
A: Some hardware may not be supported. Check the log files for details.

SUPPORT:
--------
For issues or questions, check the log files first:
%LOCALAPPDATA%\DeviceMonitor\Logs\

================================================================================
Built with LibreHardwareMonitor - Open Source Hardware Monitoring
================================================================================
