//! Trie-based router for O(path_segments) lookup

const std = @import("std");
const route = @import("route.zig");
const parsing = @import("parsing.zig");
const RouteParams = @import("RouteParams.zig");
const HttpResult = @import("HttpResult.zig");
const ServerContext = @import("ServerContext.zig");

const Router = @This();

const RouteSegment = route.RouteSegment;
const ParamType = route.ParamType;
const ParamValue = route.ParamValue;

/// Result of route matching
pub const MatchResult = struct {
    handler: HttpResult.HandlerFn,
    params: RouteParams,
};

/// Trie node for route matching
pub const RouteNode = struct {
    allocator: std.mem.Allocator,

    /// Static path children: "api" -> node, "users" -> node
    children: std.StringHashMap(*RouteNode),

    /// Single param child for {id:int}, {name:string}, etc.
    param_child: ?*RouteNode,
    param_name: ?[]const u8,
    param_type: ?ParamType,

    /// Handler if this node is a valid endpoint (per HTTP method)
    handlers: std.EnumArray(std.http.Method, ?HttpResult.HandlerFn),

    pub fn init(allocator: std.mem.Allocator) *RouteNode {
        const node = allocator.create(RouteNode) catch @panic("OOM");
        node.* = .{
            .allocator = allocator,
            .children = std.StringHashMap(*RouteNode).init(allocator),
            .param_child = null,
            .param_name = null,
            .param_type = null,
            .handlers = std.EnumArray(std.http.Method, ?HttpResult.HandlerFn).initFill(null),
        };
        return node;
    }

    pub fn deinit(self: *RouteNode) void {
        // Recursively free children
        var it = self.children.valueIterator();
        while (it.next()) |child_ptr| {
            child_ptr.*.deinit();
            self.allocator.destroy(child_ptr.*);
        }
        self.children.deinit();

        // Free param child
        if (self.param_child) |child| {
            child.deinit();
            self.allocator.destroy(child);
        }
    }

    /// Get or create a static child node
    pub fn getOrCreateChild(self: *RouteNode, segment: []const u8) *RouteNode {
        if (self.children.get(segment)) |existing| {
            return existing;
        }
        const child = RouteNode.init(self.allocator);
        self.children.put(segment, child) catch @panic("OOM");
        return child;
    }

    /// Get or create a param child node
    pub fn getOrCreateParamChild(self: *RouteNode, name: []const u8, param_type: ParamType) *RouteNode {
        if (self.param_child) |existing| {
            return existing;
        }
        const child = RouteNode.init(self.allocator);
        self.param_child = child;
        self.param_name = name;
        self.param_type = param_type;
        return child;
    }
};

allocator: std.mem.Allocator,
root: *RouteNode,

pub fn init(allocator: std.mem.Allocator) Router {
    return .{
        .allocator = allocator,
        .root = RouteNode.init(allocator),
    };
}

pub fn deinit(self: *Router) void {
    self.root.deinit();
    self.allocator.destroy(self.root);
}

/// Register a route with its handler
pub fn addRoute(
    self: *Router,
    method: std.http.Method,
    template: []const u8,
    handler: HttpResult.HandlerFn,
) !void {
    const segments = try parsing.parseTemplate(self.allocator, template);
    defer self.allocator.free(segments);

    var current = self.root;

    for (segments) |segment| {
        switch (segment) {
            .literal => |lit| {
                current = current.getOrCreateChild(lit);
            },
            .param => |p| {
                current = current.getOrCreateParamChild(p.name, p.param_type);
            },
        }
    }

    // Set handler for this method
    current.handlers.set(method, handler);
}

/// Match a path against registered routes
pub fn match(
    self: *Router,
    arena_allocator: std.mem.Allocator,
    method: std.http.Method,
    path: []const u8,
) ?MatchResult {
    const path_segments = parsing.splitPath(arena_allocator, path) catch return null;
    defer arena_allocator.free(path_segments);

    var collected_params = RouteParams.init(arena_allocator);
    errdefer collected_params.deinit();

    const node = self.matchNode(self.root, path_segments, 0, &collected_params) orelse {
        collected_params.deinit();
        return null;
    };

    const handler = node.handlers.get(method) orelse {
        collected_params.deinit();
        return null;
    };

    return MatchResult{
        .handler = handler,
        .params = collected_params,
    };
}

/// Recursively match path segments against trie nodes
fn matchNode(
    self: *Router,
    node: *RouteNode,
    segments: []const []const u8,
    index: usize,
    params: *RouteParams,
) ?*RouteNode {
    // Base case: consumed all segments
    if (index >= segments.len) {
        return node;
    }

    const segment = segments[index];

    // Priority 1: Try static children first
    if (node.children.get(segment)) |child| {
        if (self.matchNode(child, segments, index + 1, params)) |result| {
            return result;
        }
    }

    // Priority 2: Try param child
    if (node.param_child) |child| {
        if (node.param_type) |ptype| {
            if (parsing.parseValue(segment, ptype)) |value| {
                // Store param name for this match
                const name = node.param_name orelse return null;
                params.put(name, value) catch return null;

                if (self.matchNode(child, segments, index + 1, params)) |result| {
                    return result;
                }
            }
        }
    }

    return null;
}

test "Router basic static route" {
    const testing = std.testing;
    var router = Router.init(testing.allocator);
    defer router.deinit();

    const handler = struct {
        fn handle(_: *ServerContext) HttpResult {
            return HttpResult.ok("test");
        }
    }.handle;

    try router.addRoute(.GET, "/api/users", handler);

    var arena = std.heap.ArenaAllocator.init(testing.allocator);
    defer arena.deinit();

    const result = router.match(arena.allocator(), .GET, "/api/users");
    try testing.expect(result != null);
}

test "Router parameterized route" {
    const testing = std.testing;
    var router = Router.init(testing.allocator);
    defer router.deinit();

    const handler = struct {
        fn handle(_: *ServerContext) HttpResult {
            return HttpResult.ok("test");
        }
    }.handle;

    try router.addRoute(.GET, "/api/users/{id:int}", handler);

    var arena = std.heap.ArenaAllocator.init(testing.allocator);
    defer arena.deinit();

    const result = router.match(arena.allocator(), .GET, "/api/users/42").?;
    try testing.expectEqual(@as(?i64, 42), result.params.getInt("id"));
}

test "Router no match returns null" {
    const testing = std.testing;
    var router = Router.init(testing.allocator);
    defer router.deinit();

    const handler = struct {
        fn handle(_: *ServerContext) HttpResult {
            return HttpResult.ok("test");
        }
    }.handle;

    try router.addRoute(.GET, "/api/users", handler);

    var arena = std.heap.ArenaAllocator.init(testing.allocator);
    defer arena.deinit();

    const result = router.match(arena.allocator(), .GET, "/api/posts");
    try testing.expect(result == null);
}

test "Router method mismatch returns null" {
    const testing = std.testing;
    var router = Router.init(testing.allocator);
    defer router.deinit();

    const handler = struct {
        fn handle(_: *ServerContext) HttpResult {
            return HttpResult.ok("test");
        }
    }.handle;

    try router.addRoute(.GET, "/api/users", handler);

    var arena = std.heap.ArenaAllocator.init(testing.allocator);
    defer arena.deinit();

    const result = router.match(arena.allocator(), .POST, "/api/users");
    try testing.expect(result == null);
}

test "Router static priority over param" {
    const testing = std.testing;
    var router = Router.init(testing.allocator);
    defer router.deinit();

    const static_handler = struct {
        fn handle(_: *ServerContext) HttpResult {
            return HttpResult.ok("static");
        }
    }.handle;

    const param_handler = struct {
        fn handle(_: *ServerContext) HttpResult {
            return HttpResult.ok("param");
        }
    }.handle;

    try router.addRoute(.GET, "/api/users/me", static_handler);
    try router.addRoute(.GET, "/api/users/{id:string}", param_handler);

    var arena = std.heap.ArenaAllocator.init(testing.allocator);
    defer arena.deinit();

    // Should match static route "me" first
    const result = router.match(arena.allocator(), .GET, "/api/users/me").?;
    var ctx = ServerContext.init(arena.allocator(), .GET, "/api/users/me", null, null);
    defer ctx.deinit();
    const response = result.handler(&ctx);
    try testing.expectEqualStrings("static", response.body.?);
}
