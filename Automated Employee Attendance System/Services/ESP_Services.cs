using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using Automated_Employee_Attendance_System.Models;

namespace Automated_Employee_Attendance_System.Services
{
    public class ESP_Services
    {
        public Action<string>? OnStatusChanged;
        public HttpClient client = new HttpClient();
        public string espBaseUrl = "";
        private Device? currentDevice;

        public ESP_Services()
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<bool> ConnectToSavedDevice()
        {
            currentDevice = DeviceService.Load();

            if (currentDevice == null)
            {
                OnStatusChanged?.Invoke("No device configured");
                SystemServices.Log("No device configured");
                return false;
            }

          
            SystemServices.Log($"Connecting to saved device: {currentDevice.IpAddress}");

            // Verify device by ID, not just IP
            var discoveredDevice = await DiscoverDevice(currentDevice.IpAddress);

            if (discoveredDevice != null && discoveredDevice.DeviceId == currentDevice.DeviceId)
            {
                espBaseUrl = currentDevice.IpAddress;
                currentDevice.IsConnected = true;

                OnStatusChanged?.Invoke($"Connected to {currentDevice.Name}");
                SystemServices.Log($"Connected to {currentDevice.Name} (ID: {currentDevice.DeviceId})");
                return true;
            }
            else
            {
                currentDevice.IsConnected = false;

                if (discoveredDevice == null)
                {
                    OnStatusChanged?.Invoke("Device not reachable");
                    SystemServices.Log($"Device not reachable: {currentDevice.IpAddress}");
                }
                else
                {
                    OnStatusChanged?.Invoke("Wrong device detected at saved IP");
                    SystemServices.Log($"Device ID mismatch. Expected: {currentDevice.DeviceId}, Found: {discoveredDevice.DeviceId}");
                }

                return false;
            }
        }

        public async Task<List<Device>> ScanForDevices(Action<string>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            var devices = new List<Device>();

            progressCallback?.Invoke("Checking AP mode...");
            var apDevice = await DiscoverDevice("http://192.168.4.1");

            if (apDevice != null)
            {
                devices.Add(apDevice);
                progressCallback?.Invoke("ESP found in AP mode");
                SystemServices.Log($"ESP found in AP mode (ID: {apDevice.DeviceId})");
                return devices;
            }

            progressCallback?.Invoke("Scanning LAN...");
            SystemServices.Log("Scanning LAN for devices...");

            // Scan in parallel batches for much faster discovery
            const int batchSize = 30;
            using var scanClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

            for (int batchStart = 1; batchStart < 255; batchStart += batchSize)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                int batchEnd = Math.Min(batchStart + batchSize - 1, 254);
                progressCallback?.Invoke($"Scanning {batchStart}-{batchEnd}/254...");

                var tasks = new List<Task<Device?>>();
                for (int i = batchStart; i <= batchEnd; i++)
                {
                    string ip = $"http://192.168.1.{i}";
                    tasks.Add(DiscoverDeviceWithClient(scanClient, ip));
                }

                var results = await Task.WhenAll(tasks);

                foreach (var device in results)
                {
                    if (device != null)
                    {
                        devices.Add(device);
                        progressCallback?.Invoke($"Device found at {device.IpAddress}");
                        SystemServices.Log($"Device found at {device.IpAddress} (ID: {device.DeviceId})");
                    }
                }
            }

            progressCallback?.Invoke($"Scan complete. Found {devices.Count} device(s)");
            return devices;
        }

        private async Task<Device?> DiscoverDeviceWithClient(HttpClient httpClient, string baseUrl)
        {
            try
            {
                var res = await httpClient.GetAsync($"{baseUrl}/discover");

                if (!res.IsSuccessStatusCode)
                    return null;

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Extract device information including unique ID
                var deviceId = root.TryGetProperty("device_id", out var idElement)
                    ? idElement.GetString() ?? ""
                    : "";

                var deviceName = root.TryGetProperty("device", out var nameElement)
                    ? nameElement.GetString() ?? "ESP Device"
                    : "ESP Device";

                var mode = root.TryGetProperty("mode", out var modeElement)
                    ? modeElement.GetString() ?? "STA"
                    : "STA";

                if (string.IsNullOrEmpty(deviceId))
                {
                    SystemServices.Log($"Warning: Device at {baseUrl} has no device_id");
                    return null;
                }

                return new Device
                {
                    Name = mode == "AP" ? "ESP (AP Mode)" : deviceName,
                    IpAddress = baseUrl,
                    DeviceId = deviceId,
                    Mode = mode,
                    IsConnected = true
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<Device?> DiscoverDevice(string baseUrl)
        {
            return await DiscoverDeviceWithClient(client, baseUrl);
        }

        async Task<bool> Ping(string url)
        {
            try
            {
                var res = await client.GetAsync(url);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public Device? GetCurrentDevice()
        {
            return currentDevice;
        }

        public async Task<List<Attendance>> GetAttendanceRecords()
        {
            if (string.IsNullOrEmpty(espBaseUrl))
            {
                SystemServices.Log("ESP not connected - cannot fetch attendance");
                return new List<Attendance>();
            }

            try
            {
                var res = await client.GetAsync($"{espBaseUrl}/attendance");

                if (!res.IsSuccessStatusCode)
                {
                    SystemServices.Log("Failed to load attendance records");
                    return new List<Attendance>();
                }

                var json = await res.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<Attendance>>(json);

                SystemServices.Log($"Loaded {list?.Count ?? 0} attendance records from ESP");

                return list ?? new List<Attendance>();
            }
            catch (Exception ex)
            {
                SystemServices.Log($"Load attendance error: {ex.Message}");
                return new List<Attendance>();
            }
        }
    }
}