using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DeviceMonitor.Services;

namespace DeviceMonitor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private HardwareMonitorService? _monitorService;
    private DispatcherTimer? _updateTimer;
    private bool _isMonitoring;
    private bool _isRefreshing = false;  // Prevent refresh overlap
    private List<SensorData> _allSensorData = new();
    private readonly LogService _logger = LogService.Instance;

    public MainWindow()
    {
        InitializeComponent();
        InitializeMonitoring();
        
        _logger.Info("MainWindow initialized");
        _logger.CleanOldLogs(); // Clean logs older than 30 days
    }

    private void InitializeMonitoring()
    {
        // Initialize the hardware monitoring service
        _monitorService = new HardwareMonitorService();

        // Set up auto-refresh timer (default: 2 seconds, configurable via UI)
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
    }

    private void IntervalComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_updateTimer == null || IntervalComboBox == null)
            return;

        // Get selected interval based on combo box index
        int selectedIndex = IntervalComboBox.SelectedIndex;
        int intervalSeconds = selectedIndex switch
        {
            0 => 1,  // Fast
            1 => 2,  // Normal
            2 => 5,  // Eco
            3 => 10, // Slow
            _ => 2   // Default
        };

        _updateTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
        _logger.Info($"Update interval changed to {intervalSeconds} seconds");
        
        // If monitoring is active, restart timer with new interval
        if (_isMonitoring)
        {
            _updateTimer.Stop();
            _updateTimer.Start();
            StatusText.Text = $"Status: Monitoring Active (Update every {intervalSeconds}s)";
        }
    }

    private void BtnStartStop_Click(object sender, RoutedEventArgs e)
    {
        if (_isMonitoring)
        {
            StopMonitoring();
        }
        else
        {
            StartMonitoring();
        }
    }

    private void StartMonitoring()
    {
        try
        {
            _logger.Info("User clicked Start monitoring");
            _monitorService?.Start();
            _updateTimer?.Start();
            _isMonitoring = true;

            BtnStartStop.Content = "⏸ Stop";
            BtnStartStop.Background = System.Windows.Media.Brushes.Red;
            StatusText.Text = "Status: Monitoring Active";
            NoDataText.Visibility = Visibility.Collapsed;

            // Initial update
            _ = RefreshSensorDataAsync();  // Async operation, non-blocking
            _logger.Info("Monitoring started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to start monitoring", ex);
            MessageBox.Show($"Error starting monitoring:\n{ex.Message}\n\nNote: This app needs to run with administrator privileges to access hardware sensors.", 
                          "Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }
    }

    private void StopMonitoring()
    {
        _logger.Info("User clicked Stop monitoring");
        _updateTimer?.Stop();
        _monitorService?.Stop();
        _isMonitoring = false;

        BtnStartStop.Content = "▶ Start";
        BtnStartStop.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(40, 167, 69));
        StatusText.Text = "Status: Stopped";
        
        SensorListView.ItemsSource = null;
        NoDataText.Visibility = Visibility.Visible;
        SensorCountText.Text = "Sensors: 0";
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_isMonitoring)
        {
            await RefreshSensorDataAsync();
        }
    }

    private async void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        await RefreshSensorDataAsync();
    }

    private async Task RefreshSensorDataAsync()
    {
        // Prevent concurrent refresh operations
        if (_isRefreshing)
        {
            _logger.Warning("Previous refresh still in progress, skipping this cycle");
            return;
        }

        _isRefreshing = true;
        try
        {
            // Execute hardware access on background thread to prevent UI blocking
            var sensorData = await Task.Run(() => 
                _monitorService?.GetSensorData() ?? new List<SensorData>()
            );
            
            // Update UI with new sensor data
            _allSensorData = sensorData;
            ApplyFilter();
            SensorCountText.Text = $"Sensors: {_allSensorData.Count}";
            
            // Show update interval in status
            double intervalSeconds = _updateTimer?.Interval.TotalSeconds ?? 2.0;
            StatusText.Text = $"Status: Monitoring Active ({intervalSeconds}s interval) - Last update: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _logger.Error("Error refreshing sensor data", ex);
            StatusText.Text = $"Error: {ex.Message}";
            // Stop monitoring on critical errors to prevent crashes
            if (ex is AccessViolationException || ex is OutOfMemoryException)
            {
                StopMonitoring();
                MessageBox.Show($"Critical error occurred. Monitoring stopped.\n\n{ex.Message}", 
                              "Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void FilterChanged(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void BtnViewLogs_Click(object sender, RoutedEventArgs e)
    {
        _logger.Info("User opened log folder");
        _logger.OpenLogFolder();
    }

    private void LogPathText_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _logger.Info("User clicked log path text");
        _logger.OpenLogFolder();
    }

    private void ApplyFilter()
    {
        if (_allSensorData == null || _allSensorData.Count == 0)
            return;

        IEnumerable<SensorData> filteredData = _allSensorData;

        if (RadioTemp.IsChecked == true)
            filteredData = _allSensorData.Where(s => s.SensorType == "Temperature");
        else if (RadioFan.IsChecked == true)
            filteredData = _allSensorData.Where(s => s.SensorType == "Fan");
        else if (RadioLoad.IsChecked == true)
            filteredData = _allSensorData.Where(s => s.SensorType == "Load");
        else if (RadioPower.IsChecked == true)
            filteredData = _allSensorData.Where(s => s.SensorType == "Power");
        else if (RadioClock.IsChecked == true)
            filteredData = _allSensorData.Where(s => s.SensorType == "Clock");

        SensorListView.ItemsSource = filteredData.ToList();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _logger.Info("Application closing");
        _updateTimer?.Stop();
        _monitorService?.Dispose();
        _logger.Info("Application closed successfully");
    }
}
