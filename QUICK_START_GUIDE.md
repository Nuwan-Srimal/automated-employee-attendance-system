# Quick Start Guide - Device Management

## For End Users

### Adding Your First Device

1. **Start the Application**
   - The status bar will show "No device configured"

2. **Open Settings**
   - Click the Settings tab in the left menu

3. **Add Device**
   - Click the "Add Device" button
   - Wait for the scan to complete (shows progress)

4. **ESP in AP Mode (First Time)**
   - If ESP is found in AP mode:
     - Click "Yes" to configure WiFi
     - Select your WiFi network from the list
     - Enter WiFi password
     - Click "Connect"
     - Wait for ESP to restart and join network
     - Device will be automatically saved

5. **ESP in STA Mode (Already Configured)**
   - Device will be found and saved automatically
   - Shows in Device List immediately

### Managing Devices

**View Device:**
- Settings ? Device Management
- Shows: Device name and IP address

**Delete Device:**
- Click the "Delete" button next to the device
- Confirm deletion
- Device removed from configuration

**Change Device:**
- Delete current device
- Click "Add Device" to scan for new device

## For Developers

### Code Examples

#### Connect to Saved Device
```csharp
var espService = new ESP_Services();
await espService.ConnectToSavedDevice();

if (!string.IsNullOrEmpty(espService.espBaseUrl))
{
    // Device connected, use espBaseUrl
}
```

#### Scan for Devices
```csharp
var devices = await espService.ScanForDevices((progress) =>
{
    Console.WriteLine(progress); // Show progress
});

if (devices.Count > 0)
{
    DeviceService.Save(devices[0]);
}
```

#### Check Device Status
```csharp
var device = DeviceService.Load();

if (device != null)
{
    Console.WriteLine($"Device: {device.Name}");
    Console.WriteLine($"IP: {device.IpAddress}");
    Console.WriteLine($"Mode: {device.Mode}");
}
```

## Troubleshooting

### "No device configured"
**Solution:** Add device through Settings ? Device Management

### "Device not reachable"
**Causes:**
- ESP is powered off
- ESP not connected to network
- IP address changed
- Firewall blocking connection

**Solution:**
1. Check ESP is powered and connected
2. Delete device from Settings
3. Add device again to find new IP

### "No ESP devices found"
**Causes:**
- ESP not powered on
- ESP not in AP mode or on same network
- Network firewall blocking discovery

**Solution:**
1. Ensure ESP is powered on
2. If first setup, ESP should be in AP mode
3. Check ESP LCD display for status
4. Try connecting to "Attendance-SETUP" WiFi manually

### "WiFi configuration failed"
**Causes:**
- Wrong WiFi password
- WiFi network out of range
- ESP firmware missing `/scanwifi` endpoint

**Solution:**
1. Verify WiFi password
2. Move ESP closer to router
3. Update ESP firmware (see ESP_FIRMWARE_UPDATE.md)

## API Endpoints Reference

### ESP Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/discover` | GET | Check if ESP is online |
| `/scanwifi` | GET | Get list of WiFi networks (AP mode) |
| `/setwifi` | POST | Configure ESP WiFi credentials (AP mode) |
| `/scanFingerprint` | GET | Enroll fingerprint |
| `/addEmployee` | POST | Save employee data |
| `/employees` | GET | Get all employees |
| `/deleteEmployee` | POST | Delete employee |
| `/attendance` | GET | Get attendance records |

### Device Service Methods

| Method | Purpose |
|--------|---------|
| `DeviceService.Load()` | Load saved device |
| `DeviceService.Save(device)` | Save device configuration |
| `DeviceService.Delete()` | Remove saved device |

### ESP Service Methods

| Method | Purpose |
|--------|---------|
| `ConnectToSavedDevice()` | Connect to saved device |
| `ScanForDevices(callback)` | Scan for available devices |
| `GetCurrentDevice()` | Get current connected device |

## Configuration Files

### Device Configuration
**Location:** `Savers/device.json`

**Example:**
```json
{
  "Name": "ESP Device",
  "IpAddress": "http://192.168.1.100",
  "Mode": "STA",
  "IsConnected": true
}
```

## System Requirements

### Application
- .NET 8
- Windows 7 or higher
- Network access to ESP device

### ESP Device
- ESP8266 with updated firmware
- WiFi capability
- HTTP web server
- Fingerprint sensor (AS608)
- SD card module
- RTC module (DS3231)
- LCD display (I2C)

## Support

### Common Questions

**Q: Can I have multiple devices?**
A: Currently, only one device can be saved at a time. Delete the current device to add a new one.

**Q: What happens if ESP IP changes?**
A: Delete the device and add it again to find the new IP.

**Q: Do I need to reconfigure every time I restart the app?**
A: No, device configuration is saved and loaded automatically.

**Q: Can I manually enter the IP address?**
A: Not in current version. Use the scan feature to discover devices.

**Q: What if I forget to update ESP firmware?**
A: WiFi scanning won't work, but you can still add devices in STA mode if you manually configure WiFi on ESP.
