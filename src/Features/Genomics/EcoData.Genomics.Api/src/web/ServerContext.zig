//! Request context provided to handlers
//! Contains request information and an arena-backed allocator for per-request allocations

const std = @import("std");
const RouteParams = @import("RouteParams.zig");

const ServerContext = @This();

method: std.http.Method,
path: []const u8,
body: ?[]const u8,
params: RouteParams,
allocator: std.mem.Allocator,
query_string: ?[]const u8,

/// Initialize a new server context
/// Note: Typically created internally by the framework, not by users
pub fn init(
    allocator: std.mem.Allocator,
    method: std.http.Method,
    path: []const u8,
    body: ?[]const u8,
    query_string: ?[]const u8,
) ServerContext {
    return .{
        .method = method,
        .path = path,
        .body = body,
        .params = RouteParams.init(allocator),
        .allocator = allocator,
        .query_string = query_string,
    };
}

/// Clean up context resources
pub fn deinit(self: *ServerContext) void {
    self.params.deinit();
}

/// Get the raw path without query string
pub fn getPath(self: *const ServerContext) []const u8 {
    return self.path;
}

/// Check if request has a body
pub fn hasBody(self: *const ServerContext) bool {
    return self.body != null and self.body.?.len > 0;
}

/// Get body as string, or empty string if no body
pub fn getBodyString(self: *const ServerContext) []const u8 {
    return self.body orelse "";
}

test "ServerContext init and deinit" {
    const testing = std.testing;
    var ctx = ServerContext.init(
        testing.allocator,
        .GET,
        "/api/test",
        null,
        null,
    );
    defer ctx.deinit();

    try testing.expectEqual(std.http.Method.GET, ctx.method);
    try testing.expectEqualStrings("/api/test", ctx.path);
    try testing.expect(ctx.body == null);
    try testing.expect(!ctx.hasBody());
}

test "ServerContext with body" {
    const testing = std.testing;
    const body = "{\"key\":\"value\"}";
    var ctx = ServerContext.init(
        testing.allocator,
        .POST,
        "/api/data",
        body,
        null,
    );
    defer ctx.deinit();

    try testing.expect(ctx.hasBody());
    try testing.expectEqualStrings(body, ctx.getBodyString());
}

test "ServerContext with query string" {
    const testing = std.testing;
    var ctx = ServerContext.init(
        testing.allocator,
        .GET,
        "/api/search",
        null,
        "q=test&limit=10",
    );
    defer ctx.deinit();

    try testing.expectEqualStrings("q=test&limit=10", ctx.query_string.?);
}
