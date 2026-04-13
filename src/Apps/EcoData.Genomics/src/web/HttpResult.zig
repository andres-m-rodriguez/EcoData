//! HTTP response result returned by handlers

const std = @import("std");
const ServerContext = @import("ServerContext.zig");

const HttpResult = @This();

/// Content types for HTTP responses
pub const ContentType = enum {
    text_plain,
    application_json,
    text_html,
    application_octet_stream,

    pub fn toHeaderValue(self: ContentType) []const u8 {
        return switch (self) {
            .text_plain => "text/plain",
            .application_json => "application/json",
            .text_html => "text/html",
            .application_octet_stream => "application/octet-stream",
        };
    }
};

/// Handler function signature
pub const HandlerFn = *const fn (ctx: *ServerContext) HttpResult;

status: std.http.Status,
body: ?[]const u8,
content_type: ContentType,

/// Create a 200 OK response with plain text body
pub fn ok(body: []const u8) HttpResult {
    return .{
        .status = .ok,
        .body = body,
        .content_type = .text_plain,
    };
}

/// Create a 200 OK response with JSON body
pub fn json(body: []const u8) HttpResult {
    return .{
        .status = .ok,
        .body = body,
        .content_type = .application_json,
    };
}

/// Create a 200 OK response with HTML body
pub fn html(body: []const u8) HttpResult {
    return .{
        .status = .ok,
        .body = body,
        .content_type = .text_html,
    };
}

/// Create a 404 Not Found response
pub fn notFound() HttpResult {
    return .{
        .status = .not_found,
        .body = "Not Found",
        .content_type = .text_plain,
    };
}

/// Create a 400 Bad Request response
pub fn badRequest(message: ?[]const u8) HttpResult {
    return .{
        .status = .bad_request,
        .body = message orelse "Bad Request",
        .content_type = .text_plain,
    };
}

/// Create a 500 Internal Server Error response
pub fn internalError(message: ?[]const u8) HttpResult {
    return .{
        .status = .internal_server_error,
        .body = message orelse "Internal Server Error",
        .content_type = .text_plain,
    };
}

/// Create a 201 Created response
pub fn created(body: ?[]const u8) HttpResult {
    return .{
        .status = .created,
        .body = body,
        .content_type = .application_json,
    };
}

/// Create a 204 No Content response
pub fn noContent() HttpResult {
    return .{
        .status = .no_content,
        .body = null,
        .content_type = .text_plain,
    };
}

/// Get the body length for Content-Length header
pub fn bodyLength(self: HttpResult) usize {
    return if (self.body) |b| b.len else 0;
}

test "HttpResult.ok" {
    const result = HttpResult.ok("Hello");
    try std.testing.expectEqual(std.http.Status.ok, result.status);
    try std.testing.expectEqualStrings("Hello", result.body.?);
    try std.testing.expectEqual(ContentType.text_plain, result.content_type);
}

test "HttpResult.json" {
    const result = HttpResult.json("{\"key\":\"value\"}");
    try std.testing.expectEqual(std.http.Status.ok, result.status);
    try std.testing.expectEqual(ContentType.application_json, result.content_type);
}

test "HttpResult.notFound" {
    const result = HttpResult.notFound();
    try std.testing.expectEqual(std.http.Status.not_found, result.status);
}

test "HttpResult.badRequest" {
    const result = HttpResult.badRequest("Invalid input");
    try std.testing.expectEqual(std.http.Status.bad_request, result.status);
    try std.testing.expectEqualStrings("Invalid input", result.body.?);
}

test "HttpResult.internalError" {
    const result = HttpResult.internalError(null);
    try std.testing.expectEqual(std.http.Status.internal_server_error, result.status);
    try std.testing.expectEqualStrings("Internal Server Error", result.body.?);
}
