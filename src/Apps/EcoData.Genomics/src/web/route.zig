const std = @import("std");

/// Supported parameter types for route templates
pub const ParamType = enum {
    int,
    string,
    guid,
    float,
};

/// Type-safe parameter value extracted from URL path
pub const ParamValue = union(ParamType) {
    int: i64,
    string: []const u8,
    guid: [16]u8,
    float: f64,

    pub fn format(
        self: ParamValue,
        comptime fmt: []const u8,
        options: std.fmt.FormatOptions,
        writer: anytype,
    ) !void {
        _ = fmt;
        _ = options;
        switch (self) {
            .int => |v| try writer.print("{d}", .{v}),
            .string => |v| try writer.print("{s}", .{v}),
            .guid => |v| {
                // Format as 8-4-4-4-12
                try writer.print("{x:0>2}{x:0>2}{x:0>2}{x:0>2}-{x:0>2}{x:0>2}-{x:0>2}{x:0>2}-{x:0>2}{x:0>2}-{x:0>2}{x:0>2}{x:0>2}{x:0>2}{x:0>2}{x:0>2}", .{
                    v[0],  v[1],  v[2],  v[3],
                    v[4],  v[5],
                    v[6],  v[7],
                    v[8],  v[9],
                    v[10], v[11], v[12], v[13], v[14], v[15],
                });
            },
            .float => |v| try writer.print("{d}", .{v}),
        }
    }
};

/// A segment in a route template - either literal text or a typed parameter
pub const RouteSegment = union(enum) {
    literal: []const u8,
    param: struct {
        name: []const u8,
        param_type: ParamType,
    },

    pub fn isParam(self: RouteSegment) bool {
        return self == .param;
    }

    pub fn isLiteral(self: RouteSegment) bool {
        return self == .literal;
    }
};

/// A complete route definition with its parsed segments
pub const Route = struct {
    template: []const u8,
    segments: []const RouteSegment,
    method: std.http.Method,

    pub fn segmentCount(self: Route) usize {
        return self.segments.len;
    }
};

test "ParamValue format" {
    const testing = std.testing;
    var buf: [100]u8 = undefined;

    // Test int formatting
    const int_val = ParamValue{ .int = 42 };
    const int_str = try std.fmt.bufPrint(&buf, "{}", .{int_val});
    try testing.expectEqualStrings("42", int_str);

    // Test string formatting
    const str_val = ParamValue{ .string = "hello" };
    const str_str = try std.fmt.bufPrint(&buf, "{}", .{str_val});
    try testing.expectEqualStrings("hello", str_str);

    // Test float formatting
    const float_val = ParamValue{ .float = 3.14 };
    const float_str = try std.fmt.bufPrint(&buf, "{}", .{float_val});
    try testing.expect(float_str.len > 0);
}

test "RouteSegment predicates" {
    const testing = std.testing;

    const literal = RouteSegment{ .literal = "api" };
    try testing.expect(literal.isLiteral());
    try testing.expect(!literal.isParam());

    const param = RouteSegment{ .param = .{ .name = "id", .param_type = .int } };
    try testing.expect(param.isParam());
    try testing.expect(!param.isLiteral());
}
