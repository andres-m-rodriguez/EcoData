using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
var mappingsPath = Path.Combine(repoRoot, "deployable-resources.json");

if (!File.Exists(mappingsPath))
{
    Console.WriteLine("Error: deployable-resources.json not found. Run generate-deploy-mappings.cs first.");
    return 1;
}

// Get affected projects from dotnet affected
Console.WriteLine("Running dotnet affected...\n");
var affectedTxt = Path.Combine(repoRoot, "affected.txt");
Run("dotnet", $"affected --format text -o \"{affectedTxt}\"");

if (!File.Exists(affectedTxt) || new FileInfo(affectedTxt).Length == 0)
{
    Console.WriteLine("No affected projects detected.");
    return 0;
}

var affectedProjects = File.ReadAllLines(affectedTxt)
    .Where(line => !string.IsNullOrWhiteSpace(line))
    .Select(line => Path.GetFileNameWithoutExtension(line))
    .ToHashSet();

Console.WriteLine($"Affected projects ({affectedProjects.Count}):");
foreach (var p in affectedProjects)
    Console.WriteLine($"  - {p}");

// Load mappings
var mappings = JsonSerializer.Deserialize(File.ReadAllText(mappingsPath), JsonCtx.Default.Mappings)!;

// Find which services need to be deployed
var servicesToDeploy = affectedProjects
    .Where(p => mappings.ProjectToResources.ContainsKey(p))
    .SelectMany(p => mappings.ProjectToResources[p])
    .Distinct()
    .Order()
    .ToList();

if (servicesToDeploy.Count == 0)
{
    Console.WriteLine("\nNo deployable services affected.");
    return 0;
}

Console.WriteLine($"\nServices to deploy ({servicesToDeploy.Count}):");
foreach (var s in servicesToDeploy)
    Console.WriteLine($"  - {s}");

// Deploy each affected service
Console.WriteLine("\nDeploying...\n");
var failed = new List<string>();

foreach (var service in servicesToDeploy)
{
    Console.WriteLine($"=== Deploying {service} ===");
    var exitCode = RunWithOutput("aspire", $"do deploy-{service}");

    if (exitCode != 0)
        failed.Add(service);

    Console.WriteLine();
}

// Summary
if (failed.Count > 0)
{
    Console.WriteLine($"Failed deployments: {string.Join(", ", failed)}");
    return 1;
}

Console.WriteLine("All deployments successful!");
return 0;

// --- Helpers ---

static string FindRepoRoot(string path)
{
    for (var dir = new DirectoryInfo(path); dir is not null; dir = dir.Parent)
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            return dir.FullName;
    return path;
}

static string Run(string cmd, string args)
{
    var psi = new ProcessStartInfo(cmd, args)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    using var proc = Process.Start(psi)!;
    var output = proc.StandardOutput.ReadToEnd();
    proc.WaitForExit();
    return output;
}

static int RunWithOutput(string cmd, string args)
{
    var psi = new ProcessStartInfo(cmd, args)
    {
        UseShellExecute = false
    };

    using var proc = Process.Start(psi)!;
    proc.WaitForExit();
    return proc.ExitCode;
}

// --- Types ---

record Mappings(
    Dictionary<string, Resource> Resources,
    Dictionary<string, List<string>> ProjectToResources
);

record Resource(string Project, List<string> Dependencies);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Mappings))]
partial class JsonCtx : JsonSerializerContext;
