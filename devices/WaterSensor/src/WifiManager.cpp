#include "WifiManager.h"

WifiManager* WifiManager::_instance = nullptr;

WifiManager::WifiManager()
    : _state(WifiState::Disconnected)
    , _stateCallback(nullptr)
    , _connectionTimeout(10000)
    , _retryCount(3)
    , _currentRetry(0)
    , _lastConnectionAttempt(0)
    , _reconnectInterval(30000)
    , _autoReconnect(true)
{
    _instance = this;
}

WifiManager::~WifiManager()
{
    _preferences.end();
    _instance = nullptr;
}

void WifiManager::begin()
{
    WiFi.mode(WIFI_STA);
    WiFi.setAutoReconnect(false);
    WiFi.onEvent(onWifiEvent);

    Serial.println("[WifiManager] Initialized");
}

bool WifiManager::connect()
{
    auto credentials = getStoredCredentials();
    if (!credentials.has_value()) {
        Serial.println("[WifiManager] No stored credentials found");
        setState(WifiState::ConnectionFailed);
        return false;
    }

    return connect(credentials->ssid, credentials->password, false);
}

bool WifiManager::connect(const String& ssid, const String& password, bool save)
{
    if (ssid.isEmpty()) {
        Serial.println("[WifiManager] SSID cannot be empty");
        setState(WifiState::ConnectionFailed);
        return false;
    }

    Serial.printf("[WifiManager] Connecting to %s\n", ssid.c_str());
    setState(WifiState::Connecting);

    _currentRetry = 0;

    while (_currentRetry < _retryCount) {
        WiFi.disconnect(true);
        delay(100);

        WiFi.begin(ssid.c_str(), password.c_str());

        if (waitForConnection()) {
            Serial.printf("[WifiManager] Connected! IP: %s\n", getIpAddress().c_str());
            setState(WifiState::Connected);

            if (save) {
                saveCredentials(ssid, password);
            }

            return true;
        }

        _currentRetry++;
        if (_currentRetry < _retryCount) {
            Serial.printf("[WifiManager] Connection failed, retry %d/%d\n",
                _currentRetry + 1, _retryCount);
            delay(1000);
        }
    }

    Serial.println("[WifiManager] Connection failed after all retries");
    setState(WifiState::ConnectionFailed);
    return false;
}

void WifiManager::disconnect()
{
    WiFi.disconnect(true);
    setState(WifiState::Disconnected);
    Serial.println("[WifiManager] Disconnected");
}

bool WifiManager::isConnected() const
{
    return WiFi.status() == WL_CONNECTED;
}

WifiState WifiManager::getState() const
{
    return _state;
}

String WifiManager::getIpAddress() const
{
    return WiFi.localIP().toString();
}

String WifiManager::getCurrentSsid() const
{
    return WiFi.SSID();
}

int WifiManager::getRssi() const
{
    return WiFi.RSSI();
}

bool WifiManager::hasStoredCredentials() const
{
    Preferences prefs;
    prefs.begin(NVS_NAMESPACE, true);
    bool hasCredentials = prefs.isKey(NVS_SSID_KEY);
    prefs.end();
    return hasCredentials;
}

std::optional<WifiCredentials> WifiManager::getStoredCredentials() const
{
    Preferences prefs;
    prefs.begin(NVS_NAMESPACE, true);

    if (!prefs.isKey(NVS_SSID_KEY)) {
        prefs.end();
        return std::nullopt;
    }

    WifiCredentials creds;
    creds.ssid = prefs.getString(NVS_SSID_KEY, "");
    creds.password = prefs.getString(NVS_PASS_KEY, "");
    prefs.end();

    if (creds.ssid.isEmpty()) {
        return std::nullopt;
    }

    return creds;
}

void WifiManager::clearCredentials()
{
    Preferences prefs;
    prefs.begin(NVS_NAMESPACE, false);
    prefs.clear();
    prefs.end();
    Serial.println("[WifiManager] Credentials cleared");
}

void WifiManager::setConnectionTimeout(uint32_t timeout)
{
    _connectionTimeout = timeout;
}

void WifiManager::setRetryCount(uint8_t count)
{
    _retryCount = count;
}

void WifiManager::onStateChange(StateCallback callback)
{
    _stateCallback = callback;
}

void WifiManager::loop()
{
    if (!_autoReconnect) {
        return;
    }

    if (_state == WifiState::Connected && !isConnected()) {
        Serial.println("[WifiManager] Connection lost");
        setState(WifiState::Disconnected);
    }

    if (_state == WifiState::Disconnected || _state == WifiState::ConnectionFailed) {
        uint32_t now = millis();
        if (now - _lastConnectionAttempt >= _reconnectInterval) {
            _lastConnectionAttempt = now;

            if (hasStoredCredentials()) {
                Serial.println("[WifiManager] Attempting reconnection...");
                connect();
            }
        }
    }
}

void WifiManager::setState(WifiState newState)
{
    if (_state != newState) {
        _state = newState;
        if (_stateCallback) {
            _stateCallback(newState);
        }
    }
}

bool WifiManager::saveCredentials(const String& ssid, const String& password)
{
    Preferences prefs;
    prefs.begin(NVS_NAMESPACE, false);
    prefs.putString(NVS_SSID_KEY, ssid);
    prefs.putString(NVS_PASS_KEY, password);
    prefs.end();

    Serial.println("[WifiManager] Credentials saved");
    return true;
}

bool WifiManager::waitForConnection()
{
    uint32_t startTime = millis();

    while (millis() - startTime < _connectionTimeout) {
        if (WiFi.status() == WL_CONNECTED) {
            return true;
        }
        delay(100);
    }

    return false;
}

void WifiManager::onWifiEvent(WiFiEvent_t event)
{
    if (!_instance) return;

    switch (event) {
        case ARDUINO_EVENT_WIFI_STA_GOT_IP:
            Serial.printf("[WifiManager] Got IP: %s\n", WiFi.localIP().toString().c_str());
            break;

        case ARDUINO_EVENT_WIFI_STA_DISCONNECTED:
            if (_instance->_state == WifiState::Connected) {
                Serial.println("[WifiManager] WiFi disconnected");
                _instance->setState(WifiState::Disconnected);
            }
            break;

        default:
            break;
    }
}
