using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using DeviceMonitor.Services;

namespace DeviceMonitor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var logger = LogService.Instance;
        
        // Check if running with Administrator privileges
        bool isAdmin = IsRunningAsAdministrator();
        logger.Info($"Application started. Is Administrator: {isAdmin}");
        
        if (!isAdmin)
        {
            logger.Warning("Running without admin privileges. Requesting UAC elevation...");
            
            // Request Administrator privileges
            bool elevationSucceeded = RequestAdministratorPrivileges();
            
            if (!elevationSucceeded)
            {
                // User denied the UAC prompt or elevation failed
                logger.Warning("User denied UAC prompt or elevation failed");
                MessageBox.Show(
                    "Device Monitor requires Administrator privileges to access hardware sensors.\n\n" +
                    "Please right-click the application and select 'Run as administrator' to continue.",
                    "Administrator Privileges Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                logger.Info("Application shutting down due to missing admin privileges");
                Current.Shutdown();
                return;
            }
            
            // If we reach here, elevation was requested and the elevated process is starting
            logger.Info("Elevated process started. Current non-admin process shutting down");
            Current.Shutdown();
            return;
        }

        // Running with admin privileges - open main window
        logger.Info("Admin privileges confirmed. Opening main window");
        try
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            logger.Error("Failed to open main window", ex);
            MessageBox.Show($"Failed to start application:\n{ex.Message}", "Startup Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
        }
    }

    /// <summary>
    /// Check if the application is running with Administrator privileges
    /// </summary>
    private static bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking admin status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Request Administrator privileges by restarting the application with 'runas' verb
    /// </summary>
    private static bool RequestAdministratorPrivileges()
    {
        try
        {
            // Get the current executable path
            string? exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            
            // Handle cases where the app is running from a runtime
            // (e.g., when running with 'dotnet run' during development)
            if (exePath?.EndsWith(".dll") == true)
            {
                // For DLL, try to find the exe wrapper
                exePath = exePath.Replace(".dll", ".exe");
                if (!File.Exists(exePath))
                {
                    // If no exe wrapper exists, the elevation won't work via runas
                    Debug.WriteLine("Cannot find .exe file for elevation (running from 'dotnet run')");
                    return false;
                }
            }

            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                Debug.WriteLine($"Executable path not found or invalid: {exePath}");
                return false;
            }

            // Create process to restart with admin privileges
            var processInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",  // This triggers the UAC prompt
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory
            };

            // Start the process with elevated privileges
            var elevatedProcess = Process.Start(processInfo);
            
            if (elevatedProcess != null)
            {
                Debug.WriteLine($"Elevated process started with PID: {elevatedProcess.Id}");
                return true;
            }
            
            Debug.WriteLine("Failed to start elevated process");
            return false;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // This exception is thrown when user denies the UAC prompt
            Debug.WriteLine($"UAC prompt denied or elevation failed: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unexpected error requesting administrator privileges: {ex.Message}");
            return false;
        }
    }
}

