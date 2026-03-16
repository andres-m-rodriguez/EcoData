#pragma once

// =============================================================================
// SENSOR CONFIGURATION
// Copy this file to config.local.h and fill in your credentials
// config.local.h is gitignored and will not be committed
// =============================================================================

// WiFi credentials
#define WIFI_SSID "your-wifi-ssid"
#define WIFI_PASSWORD "your-wifi-password"

// API configuration
#define API_BASE_URL "https://your-api-url.com"
#define SENSOR_ID "00000000-0000-0000-0000-000000000000"
#define JWT_TOKEN "your-jwt-token"

// Timing configuration (milliseconds)
#define SEND_INTERVAL_MS 30000  // 30 seconds

// NTP configuration
#define NTP_SERVER "pool.ntp.org"
#define GMT_OFFSET_SEC 0
#define DAYLIGHT_OFFSET_SEC 0
