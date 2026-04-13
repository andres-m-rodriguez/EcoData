//! Type-safe wrapper for route parameters extracted from URL path

const std = @import("std");
const route = @import("route.zig");
const ParamValue = route.ParamValue;

const RouteParams = @This();

map: std.StringHashMap(ParamValue),

pub fn init(allocator: std.mem.Allocator) RouteParams {
    return .{
        .map = std.StringHashMap(ParamValue).init(allocator),
    };
}

pub fn deinit(self: *RouteParams) void {
    self.map.deinit();
}

/// Add a parameter value
pub fn put(self: *RouteParams, name: []const u8, value: ParamValue) !void {
    try self.map.put(name, value);
}

/// Get raw parameter value
pub fn get(self: *const RouteParams, name: []const u8) ?ParamValue {
    return self.map.get(name);
}

/// Get parameter as integer
pub fn getInt(self: *const RouteParams, name: []const u8) ?i64 {
    const value = self.map.get(name) orelse return null;
    return switch (value) {
        .int => |v| v,
        else => null,
    };
}

/// Get parameter as string
pub fn getString(self: *const RouteParams, name: []const u8) ?[]const u8 {
    const value = self.map.get(name) orelse return null;
    return switch (value) {
        .string => |v| v,
        else => null,
    };
}

/// Get parameter as GUID bytes
pub fn getGuid(self: *const RouteParams, name: []const u8) ?[16]u8 {
    const value = self.map.get(name) orelse return null;
    return switch (value) {
        .guid => |v| v,
        else => null,
    };
}

/// Get parameter as float
pub fn getFloat(self: *const RouteParams, name: []const u8) ?f64 {
    const value = self.map.get(name) orelse return null;
    return switch (value) {
        .float => |v| v,
        else => null,
    };
}

/// Check if parameter exists
pub fn contains(self: *const RouteParams, name: []const u8) bool {
    return self.map.contains(name);
}

/// Get number of parameters
pub fn count(self: *const RouteParams) usize {
    return self.map.count();
}

/// Format GUID as standard UUID string (8-4-4-4-12)
pub fn formatGuid(guid: [16]u8, buf: []u8) ![]const u8 {
    if (buf.len < 36) return error.BufferTooSmall;
    return std.fmt.bufPrint(buf, "{x:0>2}{x:0>2}{x:0>2}{x:0>2}-{x:0>2}{x:0>2}-{x:0>2}{x:0>2}-{x:0>2}{x:0>2}-{x:0>2}{x:0>2}{x:0>2}{x:0>2}{x:0>2}{x:0>2}", .{
        guid[0],  guid[1],  guid[2],  guid[3],
        guid[4],  guid[5],
        guid[6],  guid[7],
        guid[8],  guid[9],
        guid[10], guid[11], guid[12], guid[13], guid[14], guid[15],
    }) catch error.BufferTooSmall;
}

test "RouteParams basic operations" {
    const testing = std.testing;
    var params = RouteParams.init(testing.allocator);
    defer params.deinit();

    try params.put("id", .{ .int = 42 });
    try params.put("name", .{ .string = "test" });
    try params.put("price", .{ .float = 19.99 });

    try testing.expectEqual(@as(?i64, 42), params.getInt("id"));
    try testing.expectEqualStrings("test", params.getString("name").?);
    try testing.expectEqual(@as(?f64, 19.99), params.getFloat("price"));
    try testing.expect(params.contains("id"));
    try testing.expect(!params.contains("missing"));
    try testing.expectEqual(@as(usize, 3), params.count());
}

test "RouteParams type mismatch returns null" {
    const testing = std.testing;
    var params = RouteParams.init(testing.allocator);
    defer params.deinit();

    try params.put("id", .{ .int = 42 });

    try testing.expectEqual(@as(?i64, 42), params.getInt("id"));
    try testing.expect(params.getString("id") == null);
    try testing.expect(params.getFloat("id") == null);
    try testing.expect(params.getGuid("id") == null);
}

test "RouteParams formatGuid" {
    var buf: [36]u8 = undefined;
    const guid = [16]u8{ 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0 };
    const formatted = try RouteParams.formatGuid(guid, &buf);
    try std.testing.expectEqualStrings("12345678-9abc-def0-1234-56789abcdef0", formatted);
}
