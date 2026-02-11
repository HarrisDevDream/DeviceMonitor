# Logging Guide

## Overview

DeviceMonitor includes a comprehensive logging system for debugging and issue tracking. All logs are automatically saved locally and rotated daily.

## Log File Location

### Automatic Storage Path
```
C:\Users\{YourUsername}\AppData\Local\DeviceMonitor\Logs\
```

### Quick Access
1. Press `Win + R`
2. Type: `%localappdata%\DeviceMonitor\Logs`
3. Press Enter

Or from the application:
- Click **"View Logs"** button (if available)
- Check the status bar for log file path

## Log File Format

### File Naming
```
DeviceMonitor_20260212.log  (Daily rotation)
DeviceMonitor_20260213.log
DeviceMonitor_20260214.log
```

### Sample Log Content

```log
================================================================================
DeviceMonitor Started at 2026-02-12 14:30:25
================================================================================
OS: Microsoft Windows NT 10.0.26200.0
64-bit OS: True
64-bit Process: True
.NET Version: 10.0.2
Machine Name: DESKTOP-PC
User Name: YourName
Is Administrator: True
Working Directory: J:\TOOL\DeviceMonitor
Log Directory: C:\Users\YourName\AppData\Local\DeviceMonitor\Logs
================================================================================

[2026-02-12 14:30:25.123] [INFO] [Thread-1] Application started
[2026-02-12 14:30:25.456] [INFO] [Thread-1] Initializing HardwareMonitorService
[2026-02-12 14:30:26.789] [INFO] [Thread-1] Hardware monitoring started successfully
[2026-02-12 14:35:12.345] [WARNING] [Thread-3] SubHardware error: Timeout
[2026-02-12 14:40:30.678] [ERROR] [Thread-1] Failed to read sensor data
Stack trace follows...
```

## Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| **INFO** | Normal operations | App started, monitoring started |
| **WARNING** | Non-critical issues | Subhardware timeout, sensor unavailable |
| **ERROR** | Critical failures | Crash, initialization failure |

## LogService Implementation

### Singleton Pattern

The `LogService` uses a thread-safe singleton pattern:

```csharp
public class LogService
{
    private static LogService? _instance;
    private static readonly object _lock = new();
    
    public static LogService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LogService();
                }
            }
            return _instance;
        }
    }
}
```

### Usage Examples

```csharp
var logger = LogService.Instance;

// Info level
logger.Info("Application started successfully");

// Warning level
logger.Warning($"Sensor timeout: {sensorName}");

// Error level with exception
logger.Error("Failed to initialize hardware", exception);
```

## Features

### 1. Daily Rotation
- New log file created each day
- Prevents single file from growing too large
- Easy to locate logs by date

### 2. Automatic Cleanup
- Logs older than 30 days are automatically deleted
- Prevents disk space issues
- Configurable retention period

### 3. System Information
- Logs system information on startup
- OS version, .NET version, admin status
- Machine name, working directory

### 4. Thread Safety
- All logging operations are thread-safe
- Uses lock mechanism to prevent conflicts
- Safe for concurrent access

### 5. Exception Logging
- Full stack traces for errors
- Inner exception details
- Context information

## Log File Structure

### Startup Section
```log
================================================================================
DeviceMonitor Started at [DateTime]
================================================================================
[System Information]
================================================================================
```

### Regular Entries
```log
[Timestamp] [Level] [Thread] Message
```

Format breakdown:
- **Timestamp**: `2026-02-12 14:30:25.123`
- **Level**: `INFO`, `WARNING`, or `ERROR`
- **Thread**: Thread ID (e.g., `Thread-1`)
- **Message**: Log message content

## Common Log Messages

| Message | Meaning | Action Needed |
|---------|---------|---------------|
| "Application started" | App launched successfully | None |
| "Initializing HardwareMonitorService" | Starting sensor monitoring | None |
| "Hardware monitoring started successfully" | Sensors are active | None |
| "Failed to start hardware monitoring" | Initialization failed | Check admin rights |
| "SubHardware error" | Minor sensor issue | Usually safe to ignore |
| "GetSensorData critical error" | Major failure | Check logs, restart app |

## Debugging with Logs

### Scenario: App Won't Start

1. Navigate to log directory
2. Open latest log file
3. Look for ERROR entries near startup
4. Check for "Failed to start hardware monitoring"

### Scenario: Missing Sensor Data

1. Search for sensor-related WARNING messages
2. Check "SubHardware error" entries
3. Verify admin privileges in system info section

### Scenario: Crash on Specific Action

1. Note the timestamp when crash occurred
2. Find corresponding log entries
3. Look for ERROR entries before the crash
4. Check stack traces for root cause

## Best Practices

1. **Check logs first** when troubleshooting
2. **Include log files** when reporting issues
3. **Note timestamps** of problems for easier searching
4. **Don't delete recent logs** until issue is resolved
5. **Monitor log directory size** (automatic cleanup handles this)

## Configuration

### Change Log Retention Period

Edit `LogService.cs`:
```csharp
private const int LOG_RETENTION_DAYS = 30;  // Change this value
```

### Change Log Directory

Edit `LogService.cs`:
```csharp
_logDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "DeviceMonitor",  // Change app name
    "Logs"
);
```

## Troubleshooting

### Issue: No log files created

**Cause**: Permission issues or directory not created  
**Solution**: Run app as admin, check directory permissions

### Issue: Log files too large

**Cause**: Excessive logging or no rotation  
**Solution**: Verify daily rotation is working, reduce log verbosity

### Issue: Can't find log directory

**Cause**: Using wrong path or environment variable  
**Solution**: Use `%localappdata%` or check `LogService.LogDirectory` property

## Additional Resources

- See `LogService.cs` for full implementation
- Refer to main README for troubleshooting guide
- Check `ADMIN_PRIVILEGES_GUIDE.md` for permission issues
