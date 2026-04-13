const std = @import("std");
const route = @import("route.zig");
const RouteSegment = route.RouteSegment;
const ParamType = route.ParamType;
const ParamValue = route.ParamValue;

pub const ParseError = error{
    InvalidTemplate,
    InvalidParamSyntax,
    UnknownParamType,
    InvalidUuid,
    InvalidInt,
    InvalidFloat,
    OutOfMemory,
};

/// Parse a route template like "/api/users/{id:int}" into segments
pub fn parseTemplate(allocator: std.mem.Allocator, template: []const u8) ParseError![]RouteSegment {
    var segments: std.ArrayList(RouteSegment) = .empty;
    errdefer segments.deinit(allocator);

    // Split by '/' and process each part
    var iter = std.mem.splitScalar(u8, template, '/');
    while (iter.next()) |part| {
        // Skip empty parts (leading slash, trailing slash, double slashes)
        if (part.len == 0) continue;

        const segment = try parseSegment(allocator, part);
        segments.append(allocator, segment) catch return error.OutOfMemory;
    }

    return segments.toOwnedSlice(allocator) catch return error.OutOfMemory;
}

/// Parse a single segment - either a literal or a {name:type} parameter
fn parseSegment(allocator: std.mem.Allocator, part: []const u8) ParseError!RouteSegment {
    _ = allocator;

    if (part.len == 0) return error.InvalidTemplate;

    // Check if it's a parameter: {name:type}
    if (part[0] == '{' and part[part.len - 1] == '}') {
        if (part.len < 3) return error.InvalidParamSyntax;

        const inner = part[1 .. part.len - 1];

        // Find the colon separator
        const colon_idx = std.mem.indexOfScalar(u8, inner, ':');
        if (colon_idx) |idx| {
            const name = inner[0..idx];
            const type_str = inner[idx + 1 ..];

            if (name.len == 0) return error.InvalidParamSyntax;

            const param_type = parseParamType(type_str) orelse return error.UnknownParamType;

            return RouteSegment{
                .param = .{
                    .name = name,
                    .param_type = param_type,
                },
            };
        } else {
            // No type specified, default to string
            if (inner.len == 0) return error.InvalidParamSyntax;

            return RouteSegment{
                .param = .{
                    .name = inner,
                    .param_type = .string,
                },
            };
        }
    }

    // It's a literal segment
    return RouteSegment{ .literal = part };
}

/// Parse parameter type string to enum
fn parseParamType(type_str: []const u8) ?ParamType {
    const map = std.StaticStringMap(ParamType).initComptime(.{
        .{ "int", .int },
        .{ "string", .string },
        .{ "guid", .guid },
        .{ "uuid", .guid },
        .{ "float", .float },
    });
    return map.get(type_str);
}

/// Split a URL path into segments
pub fn splitPath(allocator: std.mem.Allocator, path: []const u8) ![][]const u8 {
    var segments: std.ArrayList([]const u8) = .empty;
    errdefer segments.deinit(allocator);

    var iter = std.mem.splitScalar(u8, path, '/');
    while (iter.next()) |part| {
        if (part.len == 0) continue;
        try segments.append(allocator, part);
    }

    return segments.toOwnedSlice(allocator);
}

/// Parse a path segment value according to the expected parameter type
pub fn parseValue(segment: []const u8, param_type: ParamType) ?ParamValue {
    return switch (param_type) {
        .int => {
            const value = std.fmt.parseInt(i64, segment, 10) catch return null;
            return ParamValue{ .int = value };
        },
        .string => ParamValue{ .string = segment },
        .guid => {
            const bytes = parseUuid(segment) catch return null;
            return ParamValue{ .guid = bytes };
        },
        .float => {
            const value = std.fmt.parseFloat(f64, segment) catch return null;
            return ParamValue{ .float = value };
        },
    };
}

/// Parse UUID string in 8-4-4-4-12 format to bytes
pub fn parseUuid(input: []const u8) ParseError![16]u8 {
    // Expected format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx (36 chars)
    if (input.len != 36) return error.InvalidUuid;

    // Verify dashes at correct positions
    if (input[8] != '-' or input[13] != '-' or input[18] != '-' or input[23] != '-') {
        return error.InvalidUuid;
    }

    var result: [16]u8 = undefined;
    var byte_idx: usize = 0;

    const hex_positions = [_]struct { start: usize, end: usize }{
        .{ .start = 0, .end = 8 }, // 8 chars = 4 bytes
        .{ .start = 9, .end = 13 }, // 4 chars = 2 bytes
        .{ .start = 14, .end = 18 }, // 4 chars = 2 bytes
        .{ .start = 19, .end = 23 }, // 4 chars = 2 bytes
        .{ .start = 24, .end = 36 }, // 12 chars = 6 bytes
    };

    for (hex_positions) |pos| {
        const hex_str = input[pos.start..pos.end];
        var i: usize = 0;
        while (i < hex_str.len) : (i += 2) {
            const high = hexDigitToValue(hex_str[i]) orelse return error.InvalidUuid;
            const low = hexDigitToValue(hex_str[i + 1]) orelse return error.InvalidUuid;
            result[byte_idx] = (high << 4) | low;
            byte_idx += 1;
        }
    }

    return result;
}

fn hexDigitToValue(c: u8) ?u8 {
    return switch (c) {
        '0'...'9' => c - '0',
        'a'...'f' => c - 'a' + 10,
        'A'...'F' => c - 'A' + 10,
        else => null,
    };
}

// Tests

test "parseTemplate basic route" {
    const testing = std.testing;
    const segments = try parseTemplate(testing.allocator, "/api/users");
    defer testing.allocator.free(segments);

    try testing.expectEqual(@as(usize, 2), segments.len);
    try testing.expectEqualStrings("api", segments[0].literal);
    try testing.expectEqualStrings("users", segments[1].literal);
}

test "parseTemplate with int param" {
    const testing = std.testing;
    const segments = try parseTemplate(testing.allocator, "/api/users/{id:int}");
    defer testing.allocator.free(segments);

    try testing.expectEqual(@as(usize, 3), segments.len);
    try testing.expectEqualStrings("api", segments[0].literal);
    try testing.expectEqualStrings("users", segments[1].literal);
    try testing.expect(segments[2].isParam());
    try testing.expectEqualStrings("id", segments[2].param.name);
    try testing.expectEqual(ParamType.int, segments[2].param.param_type);
}

test "parseTemplate with multiple params" {
    const testing = std.testing;
    const segments = try parseTemplate(testing.allocator, "/api/users/{userId:int}/posts/{postId:guid}");
    defer testing.allocator.free(segments);

    try testing.expectEqual(@as(usize, 5), segments.len);
    try testing.expectEqualStrings("userId", segments[2].param.name);
    try testing.expectEqual(ParamType.int, segments[2].param.param_type);
    try testing.expectEqualStrings("postId", segments[4].param.name);
    try testing.expectEqual(ParamType.guid, segments[4].param.param_type);
}

test "parseTemplate with default string type" {
    const testing = std.testing;
    const segments = try parseTemplate(testing.allocator, "/api/items/{name}");
    defer testing.allocator.free(segments);

    try testing.expectEqual(@as(usize, 3), segments.len);
    try testing.expectEqualStrings("name", segments[2].param.name);
    try testing.expectEqual(ParamType.string, segments[2].param.param_type);
}

test "splitPath" {
    const testing = std.testing;
    const segments = try splitPath(testing.allocator, "/api/users/123");
    defer testing.allocator.free(segments);

    try testing.expectEqual(@as(usize, 3), segments.len);
    try testing.expectEqualStrings("api", segments[0]);
    try testing.expectEqualStrings("users", segments[1]);
    try testing.expectEqualStrings("123", segments[2]);
}

test "parseValue int" {
    const value = parseValue("42", .int).?;
    try std.testing.expectEqual(@as(i64, 42), value.int);
}

test "parseValue negative int" {
    const value = parseValue("-123", .int).?;
    try std.testing.expectEqual(@as(i64, -123), value.int);
}

test "parseValue invalid int" {
    try std.testing.expect(parseValue("abc", .int) == null);
}

test "parseValue string" {
    const value = parseValue("hello", .string).?;
    try std.testing.expectEqualStrings("hello", value.string);
}

test "parseValue float" {
    const value = parseValue("3.14", .float).?;
    try std.testing.expectApproxEqAbs(@as(f64, 3.14), value.float, 0.001);
}

test "parseUuid valid" {
    const bytes = try parseUuid("12345678-9abc-def0-1234-56789abcdef0");
    try std.testing.expectEqual(@as(u8, 0x12), bytes[0]);
    try std.testing.expectEqual(@as(u8, 0x34), bytes[1]);
    try std.testing.expectEqual(@as(u8, 0x56), bytes[2]);
    try std.testing.expectEqual(@as(u8, 0x78), bytes[3]);
    try std.testing.expectEqual(@as(u8, 0x9a), bytes[4]);
    try std.testing.expectEqual(@as(u8, 0xbc), bytes[5]);
}

test "parseUuid invalid length" {
    try std.testing.expectError(error.InvalidUuid, parseUuid("12345678-9abc-def0"));
}

test "parseUuid missing dashes" {
    try std.testing.expectError(error.InvalidUuid, parseUuid("123456789abcdef01234567890abcdef0"));
}

test "parseUuid invalid hex" {
    try std.testing.expectError(error.InvalidUuid, parseUuid("XXXXXXXX-9abc-def0-1234-56789abcdef0"));
}
