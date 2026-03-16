#pragma once

#include <Arduino.h>
#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#include "ReadingItemDto.h"
#include <functional>

enum class HttpResult {
    Success,
    ConnectionFailed,
    Timeout,
    Unauthorized,
    BadRequest,
    ServerError,
    Unknown
};

struct SendResult {
    HttpResult result;
    int httpCode;
    String message;

    bool isSuccess() const { return result == HttpResult::Success; }
};

class SensorHttpClient {
public:
    using ResultCallback = std::function<void(const SendResult& result)>;

    SensorHttpClient(const String& baseUrl, const String& sensorId);
    ~SensorHttpClient();

    // Set JWT token for authentication
    void setAuthToken(const String& token);

    // Set timeout in milliseconds
    void setTimeout(uint32_t timeout);

    // Send a batch of readings synchronously
    SendResult sendReadings(const ReadingBatchDto& batch);

    // Send a single reading
    SendResult sendReading(const ReadingItemDto& reading);

    // Send readings asynchronously (calls callback when done)
    void sendReadingsAsync(const ReadingBatchDto& batch, ResultCallback callback);

    // Check if the server is reachable
    bool ping();

    // Get the sensor ID
    const String& getSensorId() const { return _sensorId; }

private:
    String _baseUrl;
    String _sensorId;
    String _authToken;
    uint32_t _timeout;
    WiFiClientSecure _secureClient;

    String buildJson(const ReadingBatchDto& batch);
    String formatTimestamp(time_t timestamp);
    HttpResult mapHttpCode(int code);
};
