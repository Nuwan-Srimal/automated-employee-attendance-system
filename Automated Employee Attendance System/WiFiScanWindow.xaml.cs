using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Automated_Employee_Attendance_System.Services;
using Automated_Employee_Attendance_System.Models;

namespace Automated_Employee_Attendance_System
{
    public partial class WiFiScanWindow : Window
    {
        private HttpClient client = new HttpClient();
        private string selectedSSID = "";
        private string espDeviceId = ""; // Store ESP's unique ID
        public bool WifiConfigured { get; private set; } = false;
        public Device? ConfiguredDevice { get; private set; } = null;

        public WiFiScanWindow()
        {
            InitializeComponent();
            ThemeManager.ApplyTheme(this);
            Loaded += async (s, e) => await InitializeSetup();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private async Task InitializeSetup()
        {
            // First, get the device ID from AP mode
            try
            {
                StatusText.Text = "Connecting to ESP in AP mode...";
                
                var discoverResponse = await client.GetAsync("http://192.168.4.1/discover");
                
                if (discoverResponse.IsSuccessStatusCode)
                {
                    var json = await discoverResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("device_id", out var idElement))
                    {
                        espDeviceId = idElement.GetString() ?? "";
                        SystemServices.Log($"ESP Device ID: {espDeviceId}");
                    }
                    else
                    {
                        CustomMessageBox.Show("ESP firmware is outdated!\n\nPlease update ESP firmware to support device ID.");
                        StatusText.Text = "ESP firmware update required";
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to connect to ESP: {ex.Message}";
                return;
            }

            await ScanWiFiNetworks();
        }

        private async Task ScanWiFiNetworks()
        {
            try
            {
                StatusText.Text = "Scanning WiFi networks...";
                
                var response = await client.GetAsync("http://192.168.4.1/scanwifi");
                
                if (!response.IsSuccessStatusCode)
                {
                    StatusText.Text = "Failed to scan WiFi networks";
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var networks = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);

                if (networks == null || networks.Count == 0)
                {
                    StatusText.Text = "No WiFi networks found";
                    return;
                }

                var ssidList = networks
                    .Select(n => n.ContainsKey("ssid") ? n["ssid"].GetString() : "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();

                WiFiList.ItemsSource = ssidList;
                StatusText.Text = $"Found {ssidList.Count} networks";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                SystemServices.Log($"WiFi scan error: {ex.Message}");
            }
        }

        private void WiFiList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (WiFiList.SelectedItem != null)
                selectedSSID = WiFiList.SelectedItem.ToString() ?? "";
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(selectedSSID))
            {
                CustomMessageBox.Show("Please select a WiFi network");
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                CustomMessageBox.Show("Please enter WiFi password");
                return;
            }

            if (string.IsNullOrEmpty(espDeviceId))
            {
                CustomMessageBox.Show("Device ID not detected. Please restart setup.");
                return;
            }

            try
            {
                StatusText.Text = "Sending WiFi credentials...";

                var data = new
                {
                    ssid = selectedSSID,
                    pass = PasswordBox.Password
                };

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("http://192.168.4.1/setwifi", content);

                if (!response.IsSuccessStatusCode)
                {
                    StatusText.Text = "Failed to configure WiFi";
                    return;
                }

                StatusText.Text = "WiFi configured! ESP restarting...";
                SystemServices.Log($"WiFi configured: {selectedSSID}");

                await Task.Delay(20000);

                StatusText.Text = "Searching for ESP on LAN by Device ID...";

                var cts = new CancellationTokenSource();
                var tasks = new List<Task<Device?>>();

                for (int i = 1; i < 255; i++)
                {
                    string ip = $"http://192.168.1.{i}";

                    tasks.Add(Task.Run(async () =>
                    {
                        if (cts.Token.IsCancellationRequested)
                            return null;

                        var device = await DiscoverDeviceById(ip, espDeviceId);
                        if (device != null)
                        {
                            device.IpAddress = ip;
                            cts.Cancel(); // 🔥 stop all others
                            return device;
                        }

                        return null;
                    }, cts.Token));
                }

                while (tasks.Count > 0)
                {
                    var finished = await Task.WhenAny(tasks);
                    tasks.Remove(finished);

                    var device = await finished;
                    if (device != null)
                    {
                        ConfiguredDevice = device;
                        WifiConfigured = true;

                        StatusText.Text = $"ESP found at {device.IpAddress}";
                        SystemServices.Log($"ESP found at {device.IpAddress} (ID: {espDeviceId})");

                        await Task.Delay(500);
                        DialogResult = true;
                        Close();
                        return;
                    }
                }

                StatusText.Text = "ESP not found on LAN.";
                CustomMessageBox.Show(
                    "Could not find ESP on network.\n\n" +
                    "Please check:\n" +
                    "- ESP is connected to WiFi\n" +
                    "- Computer is on same network\n" +
                    "- WiFi password was correct");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                SystemServices.Log($"WiFi config error: {ex.Message}");
            }
        }

        private async Task<Device?> DiscoverDeviceById(string baseUrl, string expectedDeviceId)
        {
            try
            {
                var res = await client.GetAsync($"{baseUrl}/discover");
                
                if (!res.IsSuccessStatusCode)
                    return null;

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Check if device_id matches
                if (root.TryGetProperty("device_id", out var idElement))
                {
                    var deviceId = idElement.GetString() ?? "";
                    
                    if (deviceId == expectedDeviceId)
                    {
                        var deviceName = root.TryGetProperty("device", out var nameElement)
                            ? nameElement.GetString() ?? "ESP Device"
                            : "ESP Device";

                        return new Device
                        {
                            Name = deviceName,
                            IpAddress = baseUrl,
                            DeviceId = deviceId,
                            Mode = "STA",
                            IsConnected = true
                        };
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
        