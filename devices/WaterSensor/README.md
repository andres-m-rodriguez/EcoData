# EcoData Water Sensor

ESP32-based water quality sensor for the EcoData platform.

## Requirements

- [PlatformIO](https://docs.platformio.org/en/latest/core/installation.html)
- ESP32 development board

## Build

```bash
# Build for ESP32
pio run -e esp32

# Build for ESP32-S3
pio run -e esp32s3

# Build for ESP32-C3
pio run -e esp32c3
```

## Upload

```bash
pio run -e esp32 -t upload
```

## Monitor

```bash
pio device monitor
```
