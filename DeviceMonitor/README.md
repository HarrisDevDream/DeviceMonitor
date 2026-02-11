# DeviceMonitor

A lightweight Windows desktop application for real-time hardware monitoring. Display CPU, GPU, and motherboard sensor data (temperatures, fan speeds, voltages, etc.) in a sleek WPF interface with gaming aesthetics.

## Quick Start

### Requirements
- Windows 10/11 (64-bit)
- .NET 10.0 Runtime
- Admin privileges (auto-requested via UAC)

### Installation
1. Download and extract the release package
2. Double-click `DeviceMonitor.exe`
3. Click "Yes" on the UAC prompt
4. Start monitoring your hardware!

### For Developers
```bash
git clone <repository-url>
cd DeviceMonitor
dotnet restore
dotnet build
dotnet run
```

## Architecture

**Layered Design**: UI (WPF) → Services (HardwareMonitorService, LogService) → LibreHardwareMonitor → Windows APIs

**Key Components**:
- **MainWindow**: WPF UI with configurable refresh intervals (1-10s)
- **HardwareMonitorService**: Manages sensor data collection (CPU, GPU, Motherboard only for stability)
- **LogService**: Centralized logging with daily rotation (`%LOCALAPPDATA%\DeviceMonitor\Logs\`)

## Design Philosophy

### 🚀 Why WPF?
- **Lower Resource Usage**: Native WPF vs. web-based (Electron) solutions
- **Better Performance**: Direct Windows API access without wrapper overhead
- **Native Experience**: True desktop application with instant hardware access

### ⚡ Asynchronous Architecture
- Timer-based updates prevent UI freezing
- Non-blocking sensor data collection
- Configurable refresh rates (1-10 seconds)

### 📋 Logging Infrastructure
- Tracks user actions and errors for debugging
- Singleton `LogService` with daily rotation
- Logs location: `%LOCALAPPDATA%\DeviceMonitor\Logs\`
- 30-day automatic cleanup

### 🎮 Gaming Aesthetic
- Dark theme design matching gaming hardware
- Reduces eye strain during extended use
- Highlights sensor data effectively

### 🔐 Smart Privilege Elevation
**Traditional**: Right-click → "Run as Administrator" → Launch  
**DeviceMonitor**: Double-click → UAC "Yes" → Launch

The app starts normally (AsInvoker), detects missing admin privileges, then automatically triggers UAC elevation—no manual steps needed.

## Configuration

**Refresh Interval**: 1s-10s (default: 2s)  
**Logs**: `%LOCALAPPDATA%\DeviceMonitor\Logs\DeviceMonitor_yyyyMMdd.log`

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Administrator Privileges Required" | Restart app and click "Yes" on UAC prompt |
| No sensor data | Check logs at `%LOCALAPPDATA%\DeviceMonitor\Logs\` |
| App crashes on startup | Verify .NET 10.0 Runtime is installed |

## Technical Details

**Dependencies**: LibreHardwareMonitor v0.9.5  
**Enabled Sensors**: CPU, GPU, Motherboard (Memory/Storage/Network/Controllers disabled for stability)  
**Language**: C# (.NET 10.0) with WPF  

For detailed guides, see [`docs/`](docs/) folder.
