const std = @import("std");
const Io = std.Io;
const File = Io.File;

pub fn main(init: std.process.Init) void {
    const io = init.io;

    var stdout = File.stdout();
    var buf: [256]u8 = undefined;
    var writer = stdout.writer(io, &buf);

    writer.interface.print("EcoData Genomics - Zig 0.16.0-dev\n", .{}) catch {};
    writer.interface.print("Bioinformatics processing module\n", .{}) catch {};
    writer.interface.flush() catch {};
}

test "basic test" {
    try std.testing.expect(true);
}
