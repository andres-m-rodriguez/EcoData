# EcoData C++ Client

Typed HTTP client for the EcoData API.

## Structure

```
devices/
├── include/
│   ├── Types.h           # Request/response types
│   ├── IHttpClient.h     # HTTP interface
│   ├── IEcoDataClient.h  # Typed client interface
│   ├── HttpClient.h      # HTTP implementation (cpp-httplib)
│   └── EcoDataClient.h   # Typed client implementation
├── src/
│   └── main.cpp          # Demo
├── vendor/
│   └── httplib.h         # cpp-httplib
└── build.bat             # Build script (Windows)
```

## Build

```bat
build.bat
```

Requires Visual Studio Developer Command Prompt.

## Usage

```cpp
#include "HttpClient.h"
#include "EcoDataClient.h"

auto http = std::make_shared<EcoData::HttpClient>();
EcoData::EcoDataClient client("http://localhost:5000", http);

// Login
auto result = client.login({"sensor-id", "api-key"});
if (result.success) {
    client.setAccessToken(result.value.accessToken);
}

// Push readings
EcoData::ReadingBatch batch;
batch.sensorId = "sensor-id";
batch.readings.push_back({"temperature", 25.5, "C", 1710000000});
client.pushReadings(batch);
```
