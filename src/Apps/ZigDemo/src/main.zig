const std = @import("std");
const Io = std.Io;
const File = Io.File;
const net = Io.net;
const http = std.http;

const PORT: u16 = 8090;

pub fn main(init: std.process.Init) void {
    const io = init.io;

    // Log startup
    var stdout = File.stdout();
    var log_buf: [256]u8 = undefined;
    var log_writer = stdout.writer(io, &log_buf);
    log_writer.interface.print("Zig Demo API starting on port {d}...\n", .{PORT}) catch {};
    log_writer.interface.flush() catch {};

    // Create server address
    const address = net.IpAddress.parse("0.0.0.0", PORT) catch {
        log_writer.interface.print("Failed to parse address\n", .{}) catch {};
        log_writer.interface.flush() catch {};
        return;
    };

    // Listen for connections
    var server = net.IpAddress.listen(&address, io, .{}) catch {
        log_writer.interface.print("Failed to listen on port {d}\n", .{PORT}) catch {};
        log_writer.interface.flush() catch {};
        return;
    };
    defer server.deinit(io);

    log_writer.interface.print("Listening on http://localhost:{d}\n", .{PORT}) catch {};
    log_writer.interface.print("Endpoints: /health, /api/hello\n", .{}) catch {};
    log_writer.interface.flush() catch {};

    // Accept loop
    while (true) {
        var stream = server.accept(io) catch {
            continue;
        };
        defer stream.close(io);

        handleConnection(&stream, io) catch {};
    }
}

fn handleConnection(stream: *net.Stream, io: Io) !void {
    var read_buf: [4096]u8 = undefined;
    var write_buf: [4096]u8 = undefined;

    var reader = stream.reader(io, &read_buf);
    var writer = stream.writer(io, &write_buf);

    var http_server = http.Server.init(&reader.interface, &writer.interface);

    const request = http_server.receiveHead() catch return;
    const path = request.head.target;

    if (std.mem.eql(u8, path, "/health")) {
        try sendJson(&writer.interface, "200 OK",
            \\{"status":"healthy","service":"zig-demo"}
        );
    } else if (std.mem.eql(u8, path, "/api/hello")) {
        try sendJson(&writer.interface, "200 OK",
            \\{"message":"Hello from Zig!","version":"0.16.0-dev (the best version)"}
        );
    } else {
        try sendJson(&writer.interface, "404 Not Found",
            \\{"error":"Not Found"}
        );
    }
}

fn sendJson(writer: *Io.Writer, status: []const u8, body: []const u8) !void {
    try writer.print("HTTP/1.1 {s}\r\n", .{status});
    try writer.writeAll("Content-Type: application/json\r\n");
    try writer.print("Content-Length: {d}\r\n", .{body.len});
    try writer.writeAll("Connection: close\r\n");
    try writer.writeAll("\r\n");
    try writer.writeAll(body);
    try writer.flush();
}
