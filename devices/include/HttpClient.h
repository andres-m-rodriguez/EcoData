#pragma once

#include "IHttpClient.h"
#include "httplib.h"
#include <regex>
#include <stdexcept>

namespace EcoData {

class HttpClient : public IHttpClient {
public:
    HttpResponse get(const std::string& url, const Headers& headers = {}) override {
        return makeRequest("GET", url, "", headers);
    }

    HttpResponse post(const std::string& url, const std::string& body, const Headers& headers = {}) override {
        return makeRequest("POST", url, body, headers);
    }

private:
    struct UrlParts {
        std::string scheme;
        std::string host;
        int port;
        std::string path;
    };

    UrlParts parseUrl(const std::string& url) {
        std::regex urlRegex(R"(^(https?):\/\/([^:\/]+)(?::(\d+))?(\/.*)?$)");
        std::smatch match;

        if (!std::regex_match(url, match, urlRegex)) {
            throw std::invalid_argument("Invalid URL: " + url);
        }

        return {
            match[1].str(),
            match[2].str(),
            match[3].matched ? std::stoi(match[3].str()) : (match[1].str() == "https" ? 443 : 80),
            match[4].matched ? match[4].str() : "/"
        };
    }

    HttpResponse makeRequest(const std::string& method, const std::string& url,
                             const std::string& body, const Headers& headers) {
        try {
            auto parts = parseUrl(url);
            std::string host = parts.scheme + "://" + parts.host + ":" + std::to_string(parts.port);

            httplib::Client client(host);
            client.set_connection_timeout(10);
            client.set_read_timeout(30);

            httplib::Headers h;
            for (const auto& [k, v] : headers) h.insert({k, v});

            httplib::Result result;
            if (method == "GET") {
                result = client.Get(parts.path, h);
            } else {
                auto ct = headers.count("Content-Type") ? headers.at("Content-Type") : "application/json";
                result = client.Post(parts.path, h, body, ct);
            }

            if (result) return { result->status, result->body };

            switch (result.error()) {
                case httplib::Error::Connection:    return { -1, "Connection failed" };
                case httplib::Error::Read:          return { -1, "Read error" };
                case httplib::Error::SSLConnection: return { -1, "SSL error" };
                default:                            return { -1, "Network error" };
            }
        } catch (const std::exception& e) {
            return { -1, std::string("Exception: ") + e.what() };
        }
    }
};

} // namespace EcoData
