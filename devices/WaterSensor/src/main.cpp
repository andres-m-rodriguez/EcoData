#include <stdio.h>
#include <optional>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "esp_system.h"
#include "nvs_flash.h"

static const char* TAG = "WaterSensor";

extern "C" void app_main(void)
{
    // Initialize NVS (required for WiFi)
    esp_err_t ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND)
    {
        ESP_ERROR_CHECK(nvs_flash_erase());
        ret = nvs_flash_init();
    }
    ESP_ERROR_CHECK(ret);

    ESP_LOGI(TAG, "EcoData Water Sensor initialized");
    ESP_LOGI(TAG, "Free heap: %lu bytes", esp_get_free_heap_size());

    // Main loop
    while (true)
    {
        ESP_LOGI(TAG, "Running...");
        vTaskDelay(pdMS_TO_TICKS(5000));
    }
}
