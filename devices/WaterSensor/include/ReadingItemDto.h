#pragma once

#include <Arduino.h>
#include <vector>
#include <optional>
#include <ctime>

struct ReadingItemDto {
    String parameter;
    std::optional<String> description;
    double value;
    String unit;
    time_t recordedAt;

    ReadingItemDto(const String& param, double val, const String& u)
        : parameter(param)
        , description(std::nullopt)
        , value(val)
        , unit(u)
        , recordedAt(time(nullptr))
    {}

    ReadingItemDto(const String& param, const String& desc, double val, const String& u)
        : parameter(param)
        , description(desc)
        , value(val)
        , unit(u)
        , recordedAt(time(nullptr))
    {}
};

struct ReadingBatchDto {
    String sensorId;
    std::vector<ReadingItemDto> readings;

    ReadingBatchDto(const String& id) : sensorId(id) {}

    void addReading(const String& parameter, double value, const String& unit) {
        readings.emplace_back(parameter, value, unit);
    }

    void addReading(const String& parameter, const String& description, double value, const String& unit) {
        readings.emplace_back(parameter, description, value, unit);
    }

    void addReading(const ReadingItemDto& reading) {
        readings.push_back(reading);
    }
};
