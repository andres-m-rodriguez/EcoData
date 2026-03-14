#pragma once

#include <string>
#include <map>

namespace EcoData {

using Headers = std::map<std::string, std::string>;

struct HttpResponse {
    int statusCode;
    std::string body;
};

class IHttpClient {
public:
    virtual ~IHttpClient() = default;

    virtual HttpResponse get(const std::string& url, const Headers& headers = {}) = 0;
    virtual HttpResponse post(const std::string& url, const std::string& body, const Headers& headers = {}) = 0;
};

} // namespace EcoData
