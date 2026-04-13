const std = @import("std");
const web = @import("web/web.zig");

/// GET / - Home page
fn home(_: *web.ServerContext) web.HttpResult {
    return web.html(
        \\<!DOCTYPE html>
        \\<html>
        \\<head><title>EcoData Genomics</title></head>
        \\<body>
        \\<h1>EcoData Genomics Service</h1>
        \\<p>Zig 0.16.0-dev Web Framework</p>
        \\</body>
        \\</html>
    );
}

/// GET /api/health - Health check endpoint
fn healthCheck(_: *web.ServerContext) web.HttpResult {
    return web.json(
        \\{"status": "healthy", "service": "EcoData.Genomics"}
    );
}

pub fn main(init: std.process.Init) void {
    const allocator = init.gpa;
    const io = init.io;

    // Read port from PORT environment variable (set by Aspire), default to 8080
    const port: u16 = if (init.environ_map.get("PORT")) |port_str|
        std.fmt.parseInt(u16, port_str, 10) catch 8080
    else
        8080;

    var builder = web.ApplicationBuilder.init(allocator);
    var app = builder.build(port);
    defer app.deinit();

    // Register routes
    app.mapGet("/", home) catch return;
    app.mapGet("/api/health", healthCheck) catch return;

    // Run the server
    app.run(io) catch {};
}

test "route handlers compile" {
    const handlers: []const web.HandlerFn = &.{ home, healthCheck };
    try std.testing.expect(handlers.len == 2);
}
