#pragma once

#include <Arduino.h>
#include <WiFi.h>
#include <Preferences.h>
#include <functional>
#include <optional>

enum class WifiState {
    Disconnected,
    Connecting,
    Connected,
    ConnectionFailed
};

struct WifiCredentials {
    String ssid;
    String password;
};

class WifiManager {
public:
    using StateCallback = std::function<void(WifiState state)>;

    WifiManager();
    ~WifiManager();

    // Initialize WiFi manager
    void begin();

    // Connect to WiFi using stored credentials
    bool connect();

    // Connect to specific network and optionally save credentials
    bool connect(const String& ssid, const String& password, bool save = true);

    // Disconnect from WiFi
    void disconnect();

    // Check if connected
    bool isConnected() const;

    // Get current state
    WifiState getState() const;

    // Get current IP address
    String getIpAddress() const;

    // Get current SSID
    String getCurrentSsid() const;

    // Get signal strength (RSSI)
    int getRssi() const;

    // Check if credentials are stored
    bool hasStoredCredentials() const;

    // Get stored credentials
    std::optional<WifiCredentials> getStoredCredentials() const;

    // Clear stored credentials
    void clearCredentials();

    // Set connection timeout (milliseconds)
    void setConnectionTimeout(uint32_t timeout);

    // Set retry count
    void setRetryCount(uint8_t count);

    // Set state change callback
    void onStateChange(StateCallback callback);

    // Call this in loop() to handle reconnection
    void loop();

private:
    static constexpr const char* NVS_NAMESPACE = "wifi";
    static constexpr const char* NVS_SSID_KEY = "ssid";
    static constexpr const char* NVS_PASS_KEY = "password";

    Preferences _preferences;
    WifiState _state;
    StateCallback _stateCallback;
    uint32_t _connectionTimeout;
    uint8_t _retryCount;
    uint8_t _currentRetry;
    uint32_t _lastConnectionAttempt;
    uint32_t _reconnectInterval;
    bool _autoReconnect;

    void setState(WifiState newState);
    bool saveCredentials(const String& ssid, const String& password);
    bool waitForConnection();
    static void onWifiEvent(WiFiEvent_t event);
    static WifiManager* _instance;
};
