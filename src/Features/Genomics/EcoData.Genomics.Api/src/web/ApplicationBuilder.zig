//! Builder pattern for configuring and creating an Application

const std = @import("std");
const Application = @import("Application.zig");

const ApplicationBuilder = @This();

allocator: std.mem.Allocator,

pub fn init(allocator: std.mem.Allocator) ApplicationBuilder {
    return .{ .allocator = allocator };
}

/// Build the Application instance
pub fn build(self: *ApplicationBuilder, port: u16) Application {
    return Application.init(self.allocator, port);
}

test "ApplicationBuilder build" {
    const testing = std.testing;
    var builder = ApplicationBuilder.init(testing.allocator);

    var app = builder.build(9000);
    defer app.deinit();

    try testing.expectEqual(@as(u16, 9000), app.port);
}
