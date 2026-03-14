#pragma once

#include "Types.h"

namespace EcoData {

class IEcoDataClient {
public:
    virtual ~IEcoDataClient() = default;

    /// Authenticate a sensor using its ID and API key.
    /// Returns an access token on success.
    virtual Result<SensorLoginResponse> login(const SensorLoginRequest& request) = 0;

    /// Push a batch of readings for a sensor.
    /// Requires prior authentication via setAccessToken().
    virtual Result<ReadingBatchResult> pushReadings(const ReadingBatch& batch) = 0;

    /// Set the access token for authenticated requests.
    virtual void setAccessToken(const std::string& token) = 0;

    /// Clear the current access token.
    virtual void clearAccessToken() = 0;
};

} // namespace EcoData
