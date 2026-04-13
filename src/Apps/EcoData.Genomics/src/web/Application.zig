//! Web application server

const std = @import("std");
const Router = @import("Router.zig");
const ServerContext = @import("ServerContext.zig");
const HttpResult = @import("HttpResult.zig");

const Application = @This();

const Io = std.Io;
const net = Io.net;
const http = std.http;

allocator: std.mem.Allocator,
router: Router,
port: u16,
max_body_size: usize = 1024 * 1024, // 1MB default

pub fn init(allocator: std.mem.Allocator, port: u16) Application {
    return .{
        .allocator = allocator,
        .router = Router.init(allocator),
        .port = port,
    };
}

pub fn initWithOptions(allocator: std.mem.Allocator, port: u16, max_body_size: usize) Application {
    return .{
        .allocator = allocator,
        .router = Router.init(allocator),
        .port = port,
        .max_body_size = max_body_size,
    };
}

pub fn deinit(self: *Application) void {
    self.router.deinit();
}

/// Register a GET route
pub fn mapGet(self: *Application, template: []const u8, handler: HttpResult.HandlerFn) !void {
    try self.router.addRoute(.GET, template, handler);
}

/// Register a POST route
pub fn mapPost(self: *Application, template: []const u8, handler: HttpResult.HandlerFn) !void {
    try self.router.addRoute(.POST, template, handler);
}

/// Register a PUT route
pub fn mapPut(self: *Application, template: []const u8, handler: HttpResult.HandlerFn) !void {
    try self.router.addRoute(.PUT, template, handler);
}

/// Register a DELETE route
pub fn mapDelete(self: *Application, template: []const u8, handler: HttpResult.HandlerFn) !void {
    try self.router.addRoute(.DELETE, template, handler);
}

/// Register a PATCH route
pub fn mapPatch(self: *Application, template: []const u8, handler: HttpResult.HandlerFn) !void {
    try self.router.addRoute(.PATCH, template, handler);
}

/// Run the HTTP server
pub fn run(self: *Application, io: Io) !void {
    const address = net.IpAddress{ .ip4 = net.Ip4Address.unspecified(self.port) };

    var listener = try net.IpAddress.listen(&address, io, .{});
    defer listener.deinit(io);

    self.printStartupMessage(io);

    // Accept loop
    while (true) {
        var stream = listener.accept(io) catch |err| {
            std.log.warn("Accept failed: {}", .{err});
            continue;
        };
        defer stream.close(io);

        self.handleConnection(io, &stream) catch |err| {
            std.log.warn("Connection handling failed: {}", .{err});
        };
    }
}

fn printStartupMessage(self: *Application, io: Io) void {
    var buf: [256]u8 = undefined;
    var stdout = Io.File.stdout();
    var writer_state = stdout.writer(io, &buf);
    writer_state.interface.print("Server listening on port {d}\n", .{self.port}) catch {};
    writer_state.interface.flush() catch {};
}

/// Handle a single HTTP connection using stdlib HTTP server
fn handleConnection(self: *Application, io: Io, stream: *net.Stream) !void {
    var read_buf: [8192]u8 = undefined;
    var write_buf: [8192]u8 = undefined;

    var reader_state = stream.reader(io, &read_buf);
    var writer_state = stream.writer(io, &write_buf);

    var server = http.Server.init(&reader_state.interface, &writer_state.interface);

    while (true) {
        var request = server.receiveHead() catch |err| switch (err) {
            error.HttpHeadersOversize => {
                try sendErrorResponse(&writer_state.interface, .request_header_fields_too_large);
                return;
            },
            error.HttpHeadersInvalid => {
                try sendErrorResponse(&writer_state.interface, .bad_request);
                return;
            },
            error.ReadFailed, error.HttpConnectionClosing, error.HttpRequestTruncated => return,
        };

        // Per-request arena allocator
        var arena = std.heap.ArenaAllocator.init(self.allocator);
        defer arena.deinit();

        // Extract path from target (strip query string)
        const target = request.head.target;
        const query_sep = std.mem.indexOf(u8, target, "?");
        const path = if (query_sep) |idx| target[0..idx] else target;
        const query = if (query_sep) |idx| target[idx + 1 ..] else null;

        // Match route
        const match_result = self.router.match(arena.allocator(), request.head.method, path);

        if (match_result) |matched| {
            // Read body if present
            var body: ?[]const u8 = null;
            if (request.head.content_length) |len| {
                if (len > 0 and len <= self.max_body_size) {
                    var body_buf: [8192]u8 = undefined;
                    const body_reader = request.readerExpectContinue(&body_buf) catch |err| switch (err) {
                        error.HttpExpectationFailed => {
                            try request.respond("Expectation Failed", .{ .status = .expectation_failed, .keep_alive = false });
                            return;
                        },
                        error.WriteFailed => return,
                    };
                    body = body_reader.readAlloc(arena.allocator(), len) catch null;
                }
            }

            var ctx = ServerContext.init(
                arena.allocator(),
                request.head.method,
                path,
                body,
                query,
            );
            ctx.params = matched.params;

            const result = matched.handler(&ctx);
            try request.respond(result.body orelse "", .{
                .status = result.status,
                .extra_headers = &.{
                    .{ .name = "Content-Type", .value = result.content_type.toHeaderValue() },
                },
                .keep_alive = request.head.keep_alive,
            });
        } else {
            try request.respond("Not Found", .{
                .status = .not_found,
                .keep_alive = request.head.keep_alive,
            });
        }

        if (!request.head.keep_alive) return;
    }
}

fn sendErrorResponse(writer: *Io.Writer, status: http.Status) !void {
    const phrase = status.phrase() orelse "Error";
    try writer.print("HTTP/1.1 {d} {s}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n", .{
        @intFromEnum(status),
        phrase,
    });
    try writer.flush();
}
