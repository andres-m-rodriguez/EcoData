#include <Arduino.h>
#include <optional>

static const char* TAG = "WaterSensor";

void setup()
{
    Serial.begin(115200);
    delay(1000);

    Serial.println("[WaterSensor] EcoData Water Sensor initialized");
    Serial.printf("[WaterSensor] Free heap: %u bytes\n", ESP.getFreeHeap());
}

void loop()
{
    Serial.println("[WaterSensor] Running...");
    delay(5000);
}
