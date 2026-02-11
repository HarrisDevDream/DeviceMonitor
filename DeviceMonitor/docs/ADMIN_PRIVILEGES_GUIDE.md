# Administrator Privileges Guide

## Overview

DeviceMonitor uses **`asInvoker`** with runtime UAC elevation for a better user experience. This guide explains why and how it works.

## Why `asInvoker` + Runtime Elevation?

### The Problem with `requireAdministrator`

**Manifest setting:**
```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

**Issues:**
- ❌ Non-admin users cannot launch the app at all
- ❌ `dotnet run` fails (dotnet.exe is not elevated)
- ❌ No graceful way to handle permission denial
- ❌ Poor user experience

### The Solution: `asInvoker` + Runtime Check

**Manifest setting:**
```xml
<requestedExecutionLevel level="asInvoker" uiAccess="false" />
```

**Benefits:**
- ✅ App launches normally for all users
- ✅ `dotnet run` works fine for development
- ✅ Prompts for elevation only when needed
- ✅ Graceful handling of user denial
- ✅ Better UX: Double-click → UAC "Yes" → Launch

## Implementation

### 1. Check Current Privilege Level

**App.xaml.cs:**
```csharp
private static bool IsRunningAsAdministrator()
{
    try
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    catch
    {
        return false;
    }
}
```

### 2. Request Elevation if Needed

**App.xaml.cs:**
```csharp
private static bool RequestAdministratorPrivileges()
{
    try
    {
        string exePath = Assembly.GetExecutingAssembly().Location;
        
        // Handle .dll case (development)
        if (exePath?.EndsWith(".dll") == true)
            exePath = exePath.Replace(".dll", ".exe");
        
        var processInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas",  // Triggers UAC prompt
            WorkingDirectory = Path.GetDirectoryName(exePath)
        };
        
        Process.Start(processInfo);
        return true;
    }
    catch (Win32Exception)
    {
        // User clicked "No" on UAC prompt
        return false;
    }
}
```

### 3. Application Startup Flow

**App.xaml.cs:**
```csharp
private void Application_Startup(object sender, StartupEventArgs e)
{
    bool isAdmin = IsRunningAsAdministrator();
    
    if (!isAdmin)
    {
        bool elevated = RequestAdministratorPrivileges();
        
        if (!elevated)
        {
            // User denied UAC
            MessageBox.Show(
                "DeviceMonitor requires Administrator privileges to access hardware sensors.\n\n" +
                "Please restart and click 'Yes' when prompted.",
                "Administrator Privileges Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }
        
        // Exit this non-elevated instance
        Current.Shutdown();
        return;
    }
    
    // Running with admin privileges - continue
    MainWindow mainWindow = new MainWindow();
    mainWindow.Show();
}
```

## Execution Flow

```
┌─────────────────────────────────┐
│ User: Double-click .exe         │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│ Windows: Launch with asInvoker  │
│ (normal user privileges)        │
└────────────┬────────────────────┘
             │
             ▼
┌─────────────────────────────────┐
│ App.xaml.cs: Check privileges   │
└────────────┬────────────────────┘
             │
        ┌────┴────┐
        │         │
        ▼         ▼
   Has Admin?   No Admin
        │         │
        │         ▼
        │    ┌──────────────────────┐
        │    │ Request elevation    │
        │    │ (Trigger UAC prompt) │
        │    └──────┬───────────────┘
        │           │
        │      ┌────┴────┐
        │      │         │
        │      ▼         ▼
        │   User       User
        │  clicks     clicks
        │   "Yes"      "No"
        │      │         │
        │      ▼         ▼
        │  ┌──────┐  ┌───────┐
        │  │New   │  │Show   │
        │  │admin │  │error  │
        │  │proc  │  │msg    │
        │  └──────┘  └───────┘
        │      │         │
        ▼      │         │
   Continue    │         │
   normally    │         │
        │      │         │
        └──────┴─────────┘
               │
               ▼
        ┌─────────────┐
        │Exit original│
        │process      │
        └─────────────┘
```

## Why LibreHardwareMonitor Needs Admin Rights

### Hardware Access Requirements

1. **WMI (Windows Management Instrumentation)**
   - Requires elevated privileges to access hardware sensors
   - Used for CPU, motherboard, and other sensor data

2. **Direct Hardware Communication**
   - Reading from hardware ports (e.g., I/O ports)
   - Accessing kernel-mode drivers
   - Windows restricts these operations to administrators

3. **Security Model**
   - Prevents malicious apps from reading hardware information
   - Protects against hardware exploits

### Alternative Approaches (Not Used)

| Approach | Why Not Used |
|----------|-------------|
| Kernel Driver | Too complex, requires signing |
| Service-based | Overkill for desktop app |
| Limited Sensors | Defeats the purpose |

## Testing

### Test Scenarios

1. **Normal User Launch**
   ```
   Expected: UAC prompt appears → User clicks "Yes" → App launches
   ```

2. **UAC Denial**
   ```
   Expected: Friendly error message → App closes gracefully
   ```

3. **Already Admin**
   ```
   Expected: No UAC prompt → App launches immediately
   ```

4. **Development (dotnet run)**
   ```
   Expected: UAC prompt → Continue normally
   ```

## Troubleshooting

### Issue: UAC prompt doesn't appear

**Cause**: UAC is disabled in Windows settings  
**Solution**: Enable UAC in Control Panel → User Accounts → Change User Account Control settings

### Issue: "This program requires elevation" error

**Cause**: Manifest set to `requireAdministrator`  
**Solution**: Change to `asInvoker` in app.manifest

### Issue: App crashes after UAC prompt

**Cause**: Two instances trying to access the same resources  
**Solution**: Ensure original instance calls `Shutdown()` after requesting elevation

## Best Practices

1. **Always check privileges at startup** - Don't assume elevation worked
2. **Handle UAC denial gracefully** - Show helpful error messages
3. **Log elevation attempts** - Helps diagnose issues
4. **Don't request elevation unnecessarily** - Only for operations that truly need it

## References

- [Windows Integrity Mechanism Design](https://docs.microsoft.com/en-us/windows/security/identity-protection/access-control/)
- [UAC Process and Interactions](https://docs.microsoft.com/en-us/windows/security/identity-protection/user-account-control/)
- [Assembly Manifests](https://docs.microsoft.com/en-us/windows/win32/sbscs/assembly-manifests)
