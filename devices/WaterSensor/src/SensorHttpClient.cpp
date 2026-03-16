#include "SensorHttpClient.h"

SensorHttpClient::SensorHttpClient(const String& baseUrl, const String& sensorId)
    : _baseUrl(baseUrl)
    , _sensorId(sensorId)
    , _timeout(10000)
{
    // Remove trailing slash from base URL
    if (_baseUrl.endsWith("/")) {
        _baseUrl.remove(_baseUrl.length() - 1);
    }

    // For development/testing, skip certificate validation
    // In production, load proper CA certificate
    _secureClient.setInsecure();
}

SensorHttpClient::~SensorHttpClient()
{
}

void SensorHttpClient::setAuthToken(const String& token)
{
    _authToken = token;
}

void SensorHttpClient::setTimeout(uint32_t timeout)
{
    _timeout = timeout;
}

SendResult SensorHttpClient::sendReadings(const ReadingBatchDto& batch)
{
    if (batch.readings.empty()) {
        return { HttpResult::BadRequest, 0, "No readings to send" };
    }

    HTTPClient http;
    String url = _baseUrl + "/api/sensors/" + _sensorId + "/readings";

    Serial.printf("[SensorHttpClient] POST %s\n", url.c_str());

    http.begin(_secureClient, url);
    http.setTimeout(_timeout);
    http.addHeader("Content-Type", "application/json");

    if (!_authToken.isEmpty()) {
        http.addHeader("Authorization", "Bearer " + _authToken);
    }

    String json = buildJson(batch);
    Serial.printf("[SensorHttpClient] Payload: %s\n", json.c_str());

    int httpCode = http.POST(json);

    SendResult result;
    result.httpCode = httpCode;

    if (httpCode > 0) {
        result.result = mapHttpCode(httpCode);
        result.message = http.getString();

        if (result.isSuccess()) {
            Serial.printf("[SensorHttpClient] Success: %d readings sent\n", batch.readings.size());
        } else {
            Serial.printf("[SensorHttpClient] Error %d: %s\n", httpCode, result.message.c_str());
        }
    } else {
        result.result = HttpResult::ConnectionFailed;
        result.message = http.errorToString(httpCode);
        Serial.printf("[SensorHttpClient] Connection failed: %s\n", result.message.c_str());
    }

    http.end();
    return result;
}

SendResult SensorHttpClient::sendReading(const ReadingItemDto& reading)
{
    ReadingBatchDto batch(_sensorId);
    batch.addReading(reading);
    return sendReadings(batch);
}

void SensorHttpClient::sendReadingsAsync(const ReadingBatchDto& batch, ResultCallback callback)
{
    // For simplicity, run synchronously on ESP32
    // Could be improved with FreeRTOS tasks for true async
    SendResult result = sendReadings(batch);
    if (callback) {
        callback(result);
    }
}

bool SensorHttpClient::ping()
{
    HTTPClient http;
    String url = _baseUrl + "/health";

    http.begin(_secureClient, url);
    http.setTimeout(5000);

    int httpCode = http.GET();
    http.end();

    return httpCode == 200;
}

String SensorHttpClient::buildJson(const ReadingBatchDto& batch)
{
    String json = "{";
    json += "\"sensorId\":\"" + batch.sensorId + "\",";
    json += "\"readings\":[";

    for (size_t i = 0; i < batch.readings.size(); i++) {
        const auto& r = batch.readings[i];

        if (i > 0) json += ",";

        json += "{";
        json += "\"parameter\":\"" + r.parameter + "\",";

        if (r.description.has_value()) {
            json += "\"description\":\"" + r.description.value() + "\",";
        } else {
            json += "\"description\":null,";
        }

        json += "\"value\":" + String(r.value, 6) + ",";
        json += "\"unit\":\"" + r.unit + "\",";
        json += "\"recordedAt\":\"" + formatTimestamp(r.recordedAt) + "\"";
        json += "}";
    }

    json += "]}";
    return json;
}

String SensorHttpClient::formatTimestamp(time_t timestamp)
{
    // Format as ISO 8601: 2024-01-15T10:30:00Z
    struct tm* timeinfo = gmtime(&timestamp);

    char buffer[25];
    snprintf(buffer, sizeof(buffer),
        "%04d-%02d-%02dT%02d:%02d:%02dZ",
        timeinfo->tm_year + 1900,
        timeinfo->tm_mon + 1,
        timeinfo->tm_mday,
        timeinfo->tm_hour,
        timeinfo->tm_min,
        timeinfo->tm_sec
    );

    return String(buffer);
}

HttpResult SensorHttpClient::mapHttpCode(int code)
{
    if (code >= 200 && code < 300) {
        return HttpResult::Success;
    } else if (code == 401 || code == 403) {
        return HttpResult::Unauthorized;
    } else if (code == 400 || code == 422) {
        return HttpResult::BadRequest;
    } else if (code >= 500) {
        return HttpResult::ServerError;
    } else if (code == -1) {
        return HttpResult::ConnectionFailed;
    } else if (code == -11) {
        return HttpResult::Timeout;
    }
    return HttpResult::Unknown;
}
