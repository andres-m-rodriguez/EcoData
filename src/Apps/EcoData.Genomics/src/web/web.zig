//! EcoData Web Framework
//!
//! An ASP.NET Core-like web framework for Zig 0.16.0-dev
//!
//! ## Example Usage
//! ```zig
//! const std = @import("std");
//! const web = @import("web/web.zig");
//!
//! fn getUser(ctx: *web.ServerContext) web.HttpResult {
//!     const id = ctx.params.getInt("id") orelse
//!         return web.badRequest("Invalid user ID");
//!     const response = std.fmt.allocPrint(ctx.allocator, "{d}", .{id})
//!         catch return web.internalError(null);
//!     return web.json(response);
//! }
//!
//! pub fn main(init: std.process.Init) void {
//!     const port: u16 = if (init.environ_map.get("PORT")) |p|
//!         std.fmt.parseInt(u16, p, 10) catch 8080
//!     else
//!         8080;
//!
//!     var builder = web.ApplicationBuilder.init(init.gpa);
//!     var app = builder.build(port);
//!     defer app.deinit();
//!
//!     app.mapGet("/api/users/{id:int}", getUser) catch return;
//!     app.run(init.io) catch {};
//! }
//! ```

const std = @import("std");

// Core types (module with multiple types)
pub const route = @import("route.zig");
pub const ParamType = route.ParamType;
pub const ParamValue = route.ParamValue;
pub const RouteSegment = route.RouteSegment;

// Types using @This() pattern - file IS the type
pub const HttpResult = @import("HttpResult.zig");
pub const ContentType = HttpResult.ContentType;
pub const HandlerFn = HttpResult.HandlerFn;

pub const RouteParams = @import("RouteParams.zig");
pub const ServerContext = @import("ServerContext.zig");
pub const Router = @import("Router.zig");
pub const Application = @import("Application.zig");
pub const ApplicationBuilder = @import("ApplicationBuilder.zig");

// Parsing utilities (module with functions)
pub const parsing = @import("parsing.zig");

// Convenience functions - module-level helpers for common HTTP responses

/// Create a 200 OK response with plain text body
pub fn ok(body: []const u8) HttpResult {
    return HttpResult.ok(body);
}

/// Create a 200 OK response with JSON body
pub fn json(body: []const u8) HttpResult {
    return HttpResult.json(body);
}

/// Create a 200 OK response with HTML body
pub fn html(body: []const u8) HttpResult {
    return HttpResult.html(body);
}

/// Create a 404 Not Found response
pub fn notFound() HttpResult {
    return HttpResult.notFound();
}

/// Create a 400 Bad Request response
pub fn badRequest(message: ?[]const u8) HttpResult {
    return HttpResult.badRequest(message);
}

/// Create a 500 Internal Server Error response
pub fn internalError(message: ?[]const u8) HttpResult {
    return HttpResult.internalError(message);
}

/// Create a 201 Created response
pub fn created(body: ?[]const u8) HttpResult {
    return HttpResult.created(body);
}

/// Create a 204 No Content response
pub fn noContent() HttpResult {
    return HttpResult.noContent();
}

test "web module exports" {
    // Verify all types are accessible
    _ = ApplicationBuilder;
    _ = Application;
    _ = Router;
    _ = ServerContext;
    _ = HttpResult;
    _ = RouteParams;
    _ = ParamType;
    _ = ParamValue;
}

test "convenience functions" {
    const result = ok("test");
    try std.testing.expectEqual(std.http.Status.ok, result.status);

    const json_result = json("{\"key\":\"value\"}");
    try std.testing.expectEqual(ContentType.application_json, json_result.content_type);

    const not_found_result = notFound();
    try std.testing.expectEqual(std.http.Status.not_found, not_found_result.status);
}
