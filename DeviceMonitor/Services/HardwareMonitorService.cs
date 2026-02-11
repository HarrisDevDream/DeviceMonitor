using System;
using System.Collections.Generic;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace DeviceMonitor.Services;

/// <summary>
/// Service to monitor hardware sensors (CPU temp, fan speeds, etc.)
/// </summary>
public class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private bool _isRunning;
    private readonly LogService _logger = LogService.Instance;

    public HardwareMonitorService()
    {
        _logger.Info("Initializing HardwareMonitorService");
        
        _computer = new Computer
        {
            IsCpuEnabled = true,        // Enable CPU monitoring
            IsGpuEnabled = true,        // Enable GPU monitoring
            IsMotherboardEnabled = true, // Enable motherboard monitoring
            IsMemoryEnabled = false,    // Disabled - can cause crashes
            IsStorageEnabled = false,   // Disabled - can cause crashes
            IsNetworkEnabled = false,   // Network not needed for temp/fan
            IsControllerEnabled = false // Disabled - often causes crashes
        };
        
        _logger.Info("Hardware monitoring configured: CPU=true, GPU=true, Motherboard=true");
    }

    /// <summary>
    /// Start monitoring hardware
    /// </summary>
    public void Start()
    {
        if (!_isRunning)
        {
            try
            {
                _logger.Info("Starting hardware monitoring...");
                _computer.Open();
                _isRunning = true;
                _logger.Info("Hardware monitoring started successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to start hardware monitoring", ex);
                throw new InvalidOperationException(
                    $"Failed to initialize hardware monitoring. Make sure the app is running as Administrator.\n\nError: {ex.Message}", 
                    ex);
            }
        }
    }

    /// <summary>
    /// Stop monitoring hardware
    /// </summary>
    public void Stop()
    {
        if (_isRunning)
        {
            _logger.Info("Stopping hardware monitoring...");
            _computer.Close();
            _isRunning = false;
            _logger.Info("Hardware monitoring stopped");
        }
    }

    /// <summary>
    /// Get all sensor data from all hardware components
    /// </summary>
    public List<SensorData> GetSensorData()
    {
        if (!_isRunning)
            return new List<SensorData>();

        var sensorDataList = new List<SensorData>();

        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                try
                {
                    hardware.Update(); // Update sensor values

                    // Get sensors from main hardware
                    AddSensorsFromHardware(hardware, sensorDataList);

                    // Get sensors from sub-hardware (e.g., GPU sub-components)
                    if (hardware.SubHardware != null && hardware.SubHardware.Length > 0)
                    {
                        foreach (var subHardware in hardware.SubHardware)
                        {
                            try
                            {
                                subHardware.Update();
                                AddSensorsFromHardware(subHardware, sensorDataList);
                            }
                            catch (Exception ex)
                            {
                                // Log but continue - don't crash on sub-hardware errors
                                _logger.Warning($"SubHardware error for {hardware.Name}: {ex.Message}");
                                System.Diagnostics.Debug.WriteLine($"SubHardware error for {hardware.Name}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue - don't crash on hardware errors
                    _logger.Warning($"Hardware error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Hardware error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("GetSensorData critical error", ex);
            System.Diagnostics.Debug.WriteLine($"GetSensorData critical error: {ex.Message}");
        }

        return sensorDataList;
    }

    private void AddSensorsFromHardware(IHardware hardware, List<SensorData> sensorDataList)
    {
        try
        {
            foreach (var sensor in hardware.Sensors)
            {
                // Only add sensors that have a value
                if (sensor.Value.HasValue)
                {
                    sensorDataList.Add(new SensorData
                    {
                        HardwareName = hardware.Name ?? "Unknown",
                        HardwareType = hardware.HardwareType.ToString(),
                        SensorName = sensor.Name ?? "Unknown Sensor",
                        SensorType = sensor.SensorType.ToString(),
                        Value = sensor.Value.Value,
                        Unit = GetSensorUnit(sensor.SensorType)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"AddSensorsFromHardware error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"AddSensorsFromHardware error: {ex.Message}");
        }
    }

    private string GetSensorUnit(SensorType sensorType)
    {
        return sensorType switch
        {
            SensorType.Temperature => "°C",
            SensorType.Fan => "RPM",
            SensorType.Voltage => "V",
            SensorType.Clock => "MHz",
            SensorType.Load => "%",
            SensorType.Power => "W",
            SensorType.Data => "GB",
            SensorType.Throughput => "MB/s",
            SensorType.Level => "%",
            _ => ""
        };
    }

    public void Dispose()
    {
        _logger.Info("Disposing HardwareMonitorService");
        Stop();
        _computer?.Close();
    }
}

/// <summary>
/// Data model for sensor information
/// </summary>
public class SensorData
{
    public string HardwareName { get; set; } = string.Empty;
    public string HardwareType { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;
    public float Value { get; set; }
    public string Unit { get; set; } = string.Empty;

    public string DisplayName => $"{HardwareName} - {SensorName}";
    public string DisplayValue => $"{Value:F1} {Unit}";
}
