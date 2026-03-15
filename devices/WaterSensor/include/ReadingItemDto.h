#pragma once

#include <string>
#include <optional>
#include <chrono>

struct ReadingItemDto {
    std::string parameter;
    std::optional<std::string> description;
    double value;
    std::string unit;
    std::chrono::system_clock::time_point recordedAt;
};
