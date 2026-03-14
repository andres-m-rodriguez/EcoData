#pragma once

#include <string>
#include <vector>
#include <cstdint>
#include <optional>

namespace EcoData {

// ============================================================================
// Auth Types
// ============================================================================

struct SensorLoginRequest {
    std::string sensorId;
    std::string apiKey;
};

struct SensorLoginResponse {
    std::string accessToken;
    int64_t expiresAtUnix;  // Unix timestamp
};

// ============================================================================
// Reading Types
// ============================================================================

struct ReadingItem {
    std::string parameter;
    double value;
    std::string unit;
    int64_t recordedAtUnix;  // Unix timestamp
};

struct ReadingBatch {
    std::string sensorId;
    std::vector<ReadingItem> readings;
};

struct ReadingBatchResult {
    int totalSubmitted;
    int accepted;
    int rejected;
    std::vector<std::string> errors;
};

// ============================================================================
// Common Types
// ============================================================================

struct ApiError {
    int statusCode;
    std::string message;
};

template<typename T>
struct Result {
    bool success;
    T value;
    ApiError error;

    static Result<T> ok(T val) {
        return { true, std::move(val), {} };
    }

    static Result<T> fail(int code, std::string msg) {
        return { false, {}, { code, std::move(msg) } };
    }
};

} // namespace EcoData
