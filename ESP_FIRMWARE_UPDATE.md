# ESP8266 Firmware Update Required

## New Endpoint Needed

Add this endpoint to your ESP8266 code to support WiFi scanning:

```cpp
// ============ SCAN WIFI NETWORKS (For Setup) ============
void handleScanWiFi() {
  lcdShowMessage("Scanning WiFi...");
  
  int n = WiFi.scanNetworks();
  
  if (n == 0) {
    server.send(200, "application/json", "[]");
    lcdShowMessage("No networks");
    return;
  }
  
  String json = "[";
  for (int i = 0; i < n; i++) {
    if (i > 0) json += ",";
    
    json += "{";
    json += "\"ssid\":\"" + WiFi.SSID(i) + "\",";
    json += "\"rssi\":" + String(WiFi.RSSI(i)) + ",";
    json += "\"encryption\":" + String(WiFi.encryptionType(i));
    json += "}";
  }
  json += "]";
  
  lcdShowMessage("Found " + String(n) + " nets");
  server.send(200, "application/json", json);
}
```

## Update the setup() function

Add this line in your AP mode setup (inside `startAP()` function):

```cpp
server.on("/scanwifi", HTTP_GET, handleScanWiFi);
```

## Complete Updated Sections

### In startAP() function:
```cpp
void startAP() {
    lcdShowMessage("AP MODE", "Attendance-SET");
    WiFi.mode(WIFI_AP);
    WiFi.softAP("Attendance-SETUP", "12345678");

    server.on("/discover", [] {
        server.send(200, "application/json", "{\"device\":\"AttendanceESP\",\"mode\":\"AP\",\"ip\":\"192.168.4.1\"}");
    });

    // ? ADD THIS NEW ENDPOINT
    server.on("/scanwifi", HTTP_GET, handleScanWiFi);

    server.on("/setwifi", HTTP_POST, [] {
        String body = server.arg("plain");
        StaticJsonDocument<128> doc;
        deserializeJson(doc, body);

        String s = doc["ssid"];
        String p = doc["pass"];

        EEPROM.begin(128);
        for (int i = 0; i < 32; i++) {
            EEPROM.write(i, i < s.length() ? s[i] : 0);
            EEPROM.write(i + 32, i < p.length() ? p[i] : 0);
        }
        EEPROM.commit();

        server.send(200, "application/json", "{\"status\":\"saved\"}");
        delay(1000);
        ESP.restart();
    });
    
    server.begin();
}
```

## How the New System Works

### First Time Setup (AP Mode):
1. ESP starts in AP mode (192.168.4.1)
2. User opens Settings ? Device Management ? Click "Add Device"
3. System finds ESP in AP mode
4. User clicks "Setup" to configure WiFi
5. WiFi scan window opens and scans available networks
6. User selects network and enters password
7. ESP saves WiFi credentials and restarts
8. System scans LAN to find ESP's new IP
9. Device is saved with the new IP address

### Subsequent Usage (STA Mode):
1. Application starts
2. Connects to saved device IP directly
3. No scanning needed
4. User can delete device from Settings if needed

## Benefits

? No automatic scanning on every app start
? Faster application startup
? Device configuration saved locally
? Easy device management through Settings
? Support for AP mode WiFi setup
? Persistent device connection

## Testing

1. Upload updated ESP firmware with `/scanwifi` endpoint
2. Reset ESP WiFi (clear EEPROM or reflash)
3. ESP will start in AP mode
4. Open application ? Settings ? Add Device
5. Follow setup wizard to configure WiFi
6. Device will be saved and reused on next startup
