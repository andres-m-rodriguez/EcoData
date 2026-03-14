#pragma once

#include "IEcoDataClient.h"
#include "IHttpClient.h"
#include <memory>
#include <sstream>
#include <iomanip>
#include <regex>

namespace EcoData {

class EcoDataClient : public IEcoDataClient {
public:
    EcoDataClient(std::string baseUrl, std::shared_ptr<IHttpClient> http)
        : baseUrl_(std::move(baseUrl)), http_(std::move(http)) {}

    void setAccessToken(const std::string& token) override { token_ = token; }
    void clearAccessToken() override { token_.clear(); }

    Result<SensorLoginResponse> login(const SensorLoginRequest& req) override {
        std::string url = baseUrl_ + "/api/auth/sensor/login";
        std::string body = R"({"sensorId":")" + req.sensorId + R"(","apiKey":")" + req.apiKey + R"("})";

        auto res = http_->post(url, body, headers(false));

        if (res.statusCode != 200) {
            return Result<SensorLoginResponse>::fail(res.statusCode, res.body);
        }

        return Result<SensorLoginResponse>::ok({
            extractString(res.body, "accessToken"),
            extractInt64(res.body, "expiresAt")
        });
    }

    Result<ReadingBatchResult> pushReadings(const ReadingBatch& batch) override {
        std::string url = baseUrl_ + "/api/sensors/push";

        std::ostringstream ss;
        ss << std::fixed << std::setprecision(6);
        ss << R"({"sensorId":")" << batch.sensorId << R"(","readings":[)";

        for (size_t i = 0; i < batch.readings.size(); ++i) {
            const auto& r = batch.readings[i];
            if (i > 0) ss << ",";
            ss << R"({"parameter":")" << r.parameter
               << R"(","value":)" << r.value
               << R"(,"unit":")" << r.unit
               << R"(","recordedAt":)" << r.recordedAtUnix << "}";
        }
        ss << "]}";

        auto res = http_->post(url, ss.str(), headers(true));

        if (res.statusCode < 200 || res.statusCode >= 300) {
            return Result<ReadingBatchResult>::fail(res.statusCode, res.body);
        }

        return Result<ReadingBatchResult>::ok({
            extractInt(res.body, "totalSubmitted"),
            extractInt(res.body, "accepted"),
            extractInt(res.body, "rejected"),
            {}
        });
    }

private:
    std::string baseUrl_;
    std::shared_ptr<IHttpClient> http_;
    std::string token_;

    Headers headers(bool auth) const {
        Headers h = {{"Content-Type", "application/json"}};
        if (auth && !token_.empty()) h["Authorization"] = "Bearer " + token_;
        return h;
    }

    static std::string extractString(const std::string& json, const std::string& key) {
        std::regex re("\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
        std::smatch m;
        return std::regex_search(json, m, re) ? m[1].str() : "";
    }

    static int64_t extractInt64(const std::string& json, const std::string& key) {
        std::regex re("\"" + key + "\"\\s*:\\s*(-?\\d+)");
        std::smatch m;
        return std::regex_search(json, m, re) ? std::stoll(m[1].str()) : 0;
    }

    static int extractInt(const std::string& json, const std::string& key) {
        return static_cast<int>(extractInt64(json, key));
    }
};

} // namespace EcoData
