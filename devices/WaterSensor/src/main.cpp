#include <Arduino.h>
#include "WifiManager.h"
#include "SensorHttpClient.h"
#include <time.h>

// Load configuration - use local config if available, otherwise template
#if __has_include("config.local.h")
    #include "config.local.h"
#else
    #include "config.h"
    #warning "Using template config.h - copy to config.local.h and add your credentials"
#endif

WifiManager wifiManager;
SensorHttpClient* httpClient = nullptr;
bool timesynced = false;
unsigned long lastSendTime = 0;
int readingCount = 0;

// Generate random float in range
float randomFloat(float min, float max) {
    return min + (float)random(0, 10000) / 10000.0f * (max - min);
}

void syncTime() {
    Serial.println("[WaterSensor] Syncing time with NTP...");
    configTime(GMT_OFFSET_SEC, DAYLIGHT_OFFSET_SEC, NTP_SERVER);

    int retries = 0;
    while (time(nullptr) < 1000000000 && retries < 30) {
        Serial.print(".");
        delay(500);
        retries++;
    }
    Serial.println();

    time_t now = time(nullptr);
    if (now > 1000000000) {
        Serial.printf("[WaterSensor] Time synced: %s", ctime(&now));
        timesynced = true;
    } else {
        Serial.println("[WaterSensor] Time sync failed!");
    }
}

void sendReadings() {
    // Generate random sensor values
    float temperature = randomFloat(20.0, 30.0);    // 20-30°C
    float ph = randomFloat(6.5, 8.5);               // 6.5-8.5 pH
    float dissolvedOxygen = randomFloat(6.0, 10.0); // 6-10 mg/L
    float turbidity = randomFloat(0.0, 50.0);       // 0-50 NTU
    float conductivity = randomFloat(200, 800);     // 200-800 µS/cm

    readingCount++;
    Serial.printf("\n[WaterSensor] === Reading #%d ===\n", readingCount);
    Serial.printf("[WaterSensor] Temperature: %.2f C\n", temperature);
    Serial.printf("[WaterSensor] pH: %.2f\n", ph);
    Serial.printf("[WaterSensor] Dissolved Oxygen: %.2f mg/L\n", dissolvedOxygen);
    Serial.printf("[WaterSensor] Turbidity: %.2f NTU\n", turbidity);
    Serial.printf("[WaterSensor] Conductivity: %.2f uS/cm\n", conductivity);

    ReadingBatchDto batch(SENSOR_ID);
    batch.addReading("temperature", temperature, "celsius");
    batch.addReading("ph", ph, "pH");
    batch.addReading("dissolved_oxygen", dissolvedOxygen, "mg/L");
    batch.addReading("turbidity", turbidity, "NTU");
    batch.addReading("conductivity", conductivity, "uS/cm");

    SendResult result = httpClient->sendReadings(batch);

    if (result.isSuccess()) {
        Serial.println("[WaterSensor] Readings sent successfully!");
    } else {
        Serial.printf("[WaterSensor] Failed: %s (HTTP %d)\n",
            result.message.c_str(), result.httpCode);
    }
}

void setup()
{
    Serial.begin(115200);
    delay(1000);

    // Seed random number generator
    randomSeed(analogRead(0) + millis());

    Serial.println("[WaterSensor] EcoData Water Sensor starting...");
    Serial.printf("[WaterSensor] Free heap: %u bytes\n", ESP.getFreeHeap());
    Serial.printf("[WaterSensor] Send interval: %lu seconds\n", SEND_INTERVAL_MS / 1000);

    // Initialize WiFi
    wifiManager.begin();
    wifiManager.setConnectionTimeout(30000);
    wifiManager.setRetryCount(5);

    Serial.printf("[WaterSensor] Connecting to WiFi: %s\n", WIFI_SSID);

    if (wifiManager.connect(WIFI_SSID, WIFI_PASSWORD, false)) {
        Serial.println("[WaterSensor] WiFi connected!");
        Serial.printf("[WaterSensor] IP: %s\n", wifiManager.getIpAddress().c_str());
        syncTime();
    } else {
        Serial.println("[WaterSensor] WiFi connection failed!");
    }

    // Initialize HTTP client
    httpClient = new SensorHttpClient(API_BASE_URL, SENSOR_ID);
    httpClient->setAuthToken(JWT_TOKEN);
    httpClient->setTimeout(15000);

    // Send first reading immediately
    if (timesynced) {
        sendReadings();
        lastSendTime = millis();
    }
}

void loop()
{
    wifiManager.loop();

    if (wifiManager.isConnected() && timesynced) {
        unsigned long now = millis();
        if (now - lastSendTime >= SEND_INTERVAL_MS) {
            sendReadings();
            lastSendTime = now;
        }
    }

    delay(100);
}
