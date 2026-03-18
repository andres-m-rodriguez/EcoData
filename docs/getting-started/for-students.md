# Getting Started: For Students

Welcome to EcoData! This guide will help you set up your environmental monitoring sensors and start collecting data for your research projects.

## Overview

As a student, you'll typically:
1. Join your school's organization
2. Register your sensor device
3. Configure your hardware (ESP32, Arduino, etc.)
4. View and analyze your collected data

---

## Step 1: Create Your Account

1. Go to the **Register** page
2. Enter your school email, display name, and password
3. Click **Create Account**

> **Tip:** Use your school email (.edu) so your professor can easily identify you.

---

## Step 2: Join Your Organization

Your professor or lab coordinator has likely already created an organization for your class or research group.

1. Go to **Organizations** in the navigation
2. Browse or search for your school/lab organization
3. Click on the organization to view details
4. Click **Request Access**
5. Wait for your professor/admin to approve your request

> **Note:** You'll receive a notification when your request is approved. You'll start with "Viewer" access, which lets you see sensors and data. Ask your professor to upgrade you to "Editor" if you need to register sensors.

---

## Step 3: Register Your Sensor

Once you have Editor permissions:

1. Go to your organization's page
2. Click the **Sensors** tab
3. Click **Add Sensor**
4. Fill in the registration form:

| Field | Description | Example |
|-------|-------------|---------|
| **External ID** | Your sensor's unique identifier | `ESP32-LAB-01` |
| **Name** | A descriptive name | `Rio Santa Catarina Station 1` |
| **Location** | Click on the map or enter coordinates | Lat: 25.6866, Lng: -100.3161 |
| **Municipality** | Search and select | Monterrey |
| **Reporting Interval** | How often your sensor sends data | Every 5 minutes |

5. Click **Register Sensor**

---

## Step 4: Save Your Credentials (IMPORTANT!)

After registration, you'll see a dialog with your sensor credentials:

```
Sensor ID: 01234567-89ab-cdef-0123-456789abcdef
Access Token: eyJhbGciOiJIUzI1NiIs...
```

**CRITICAL: Copy and save these credentials immediately!**

- The access token is shown **only once**
- Your sensor needs this token to send data
- If you lose it, you'll need to contact an admin to regenerate it

> **Recommendation:** Save these in a secure note or your project's configuration file.

---

## Step 5: Configure Your Device

### For ESP32 (Arduino IDE)

```cpp
// WiFi credentials
const char* ssid = "YOUR_WIFI_SSID";
const char* password = "YOUR_WIFI_PASSWORD";

// EcoData API configuration
const char* API_HOST = "your-ecodata-server.com";
const char* SENSOR_ID = "YOUR_SENSOR_ID_HERE";
const char* ACCESS_TOKEN = "YOUR_ACCESS_TOKEN_HERE";

// Submit a reading
void submitReading(float value, const char* parameter, const char* unit) {
    HTTPClient http;

    String url = "https://" + String(API_HOST) + "/api/sensors/" + SENSOR_ID + "/readings/";
    http.begin(url);
    http.addHeader("Content-Type", "application/json");
    http.addHeader("Authorization", "Bearer " + String(ACCESS_TOKEN));

    String payload = "{\"readings\":[{";
    payload += "\"parameter\":\"" + String(parameter) + "\",";
    payload += "\"value\":" + String(value) + ",";
    payload += "\"unit\":\"" + String(unit) + "\",";
    payload += "\"recordedAt\":\"" + getISOTimestamp() + "\"";
    payload += "}]}";

    int responseCode = http.POST(payload);
    http.end();
}
```

### Common Parameters to Monitor

| Parameter | Unit | Description |
|-----------|------|-------------|
| pH | pH | Water acidity/alkalinity (0-14 scale) |
| Temperature | °C | Water or air temperature |
| Dissolved Oxygen | mg/L | Oxygen concentration in water |
| Turbidity | NTU | Water clarity |
| Conductivity | µS/cm | Electrical conductivity |

---

## Step 6: View Your Data

Once your sensor starts reporting:

1. Go to **Sensors** in the navigation
2. Find and click on your sensor
3. You'll see:
   - **Sensor details** - Location, status, last reading time
   - **Readings list** - All recorded measurements
   - **Filters** - Filter by parameter or date range

### Understanding Sensor Health

Your sensor has a health status indicator:

| Status | Meaning |
|--------|---------|
| **Healthy** | Sensor is reporting data as expected |
| **Stale** | Sensor hasn't reported in a while (check connectivity) |
| **Unhealthy** | Sensor has been silent too long (may be offline) |
| **Unknown** | New sensor, no data received yet |

---

## Troubleshooting

### "My sensor isn't showing any readings"

1. Check your WiFi connection
2. Verify the Sensor ID and Access Token are correct
3. Check the serial monitor for error messages
4. Make sure your sensor's clock is synchronized (for timestamps)

### "I lost my access token"

Contact your organization admin (professor/lab coordinator) to regenerate your sensor credentials.

### "I can't register sensors"

You need "Editor" permissions. Ask your organization admin to upgrade your role from "Viewer" to "Editor".

### "My access request is still pending"

Your organization admin needs to approve your request. Contact them directly if it's been more than a day.

---

## Next Steps

- **Explore the map** - See all sensors on an interactive map
- **Compare data** - Use filters to analyze trends over time
- **Export data** - Download your readings for analysis in Excel or Python
- **Check health** - Monitor your sensor's connectivity status

---

## Need Help?

- Ask your professor or lab coordinator
- Check the full [User Workflows Documentation](../user-workflows.md)
- Review the API documentation for advanced usage
