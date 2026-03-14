#include "HttpClient.h"
#include "EcoDataClient.h"
#include "CLI11.hpp"
#include <iostream>

int main(int argc, char* argv[]) {
    CLI::App app{"EcoData Client"};

    std::string url, sensorId, apiKey;
    std::vector<std::string> readings;

    app.add_option("-u,--url", url, "API base URL")->required();
    app.add_option("-s,--sensor-id", sensorId, "Sensor ID")->required();
    app.add_option("-k,--api-key", apiKey, "API key")->required();
    app.add_option("-r,--reading", readings, "Reading as param,value,unit");

    CLI11_PARSE(app, argc, argv);

    auto http = std::make_shared<EcoData::HttpClient>();
    EcoData::EcoDataClient client(url, http);

    // Login
    auto login = client.login({sensorId, apiKey});
    if (!login.success) {
        std::cerr << "Login failed: " << login.error.message << "\n";
        return 1;
    }
    client.setAccessToken(login.value.accessToken);
    std::cout << "OK:LOGIN\n";

    // Push readings if provided
    if (!readings.empty()) {
        EcoData::ReadingBatch batch;
        batch.sensorId = sensorId;

        for (const auto& r : readings) {
            size_t p1 = r.find(',');
            size_t p2 = r.find(',', p1 + 1);
            if (p1 != std::string::npos && p2 != std::string::npos) {
                batch.readings.push_back({
                    r.substr(0, p1),
                    std::stod(r.substr(p1 + 1, p2 - p1 - 1)),
                    r.substr(p2 + 1),
                    std::time(nullptr)
                });
            }
        }

        auto push = client.pushReadings(batch);
        if (!push.success) {
            std::cerr << "Push failed: " << push.error.message << "\n";
            return 1;
        }
        std::cout << "OK:PUSH:" << push.value.accepted << "\n";
    }

    return 0;
}
