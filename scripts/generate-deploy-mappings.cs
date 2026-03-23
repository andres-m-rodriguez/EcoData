#:package Microsoft.CodeAnalysis.CSharp@4.*

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
var appHostPath = Path.Combine(repoRoot, "src", "Host", "EcoData.AppHost", "AppHost.cs");
var outputPath = Path.Combine(repoRoot, "deployable-resources.json");

Console.WriteLine($"Analyzing {appHostPath}...\n");

var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(appHostPath)).GetCompilationUnitRoot();

// Find all deployable projects (those with PublishAsAzureContainerApp*)
var deployables = syntax
    .DescendantNodes()
    .OfType<InvocationExpressionSyntax>()
    .Select(ParseFluentChain)
    .Where(c => c.Project is not null && c.IsDeployable)
    .ToList();

var resources = new Dictionary<string, Resource>();
var projectToResources = new Dictionary<string, HashSet<string>>();

foreach (var (resource, project, _) in deployables)
{
    var csproj = Directory.GetFiles(repoRoot, $"{project}.csproj", SearchOption.AllDirectories).FirstOrDefault();
    if (csproj is null)
    {
        Console.WriteLine($"Warning: {project}.csproj not found");
        continue;
    }

    var deps = GetProjectDependencies(csproj);
    Console.WriteLine($"{resource} -> {project} ({deps.Count} deps)");

    resources[resource!] = new(project!, deps.Order().ToList());

    foreach (var p in deps.Append(project!))
    {
        if (!projectToResources.TryGetValue(p, out var set))
            projectToResources[p] = set = [];
        set.Add(resource!);
    }
}

var output = new Output(
    resources,
    projectToResources.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value.Order().ToList())
);

File.WriteAllText(outputPath, JsonSerializer.Serialize(output, JsonCtx.Default.Output));
Console.WriteLine($"\nGenerated {outputPath}");

// --- Helpers ---

static (string? Resource, string? Project, bool IsDeployable) ParseFluentChain(InvocationExpressionSyntax node)
{
    string? resource = null, project = null;
    var isDeployable = false;

    for (var current = node; current is not null;)
    {
        if (current.Expression is MemberAccessExpressionSyntax { Name: var name } access)
        {
            var method = name.Identifier.Text;

            if (method == "AddProject" && name is GenericNameSyntax { TypeArgumentList.Arguments: [QualifiedNameSyntax type] })
            {
                project = type.Right.Identifier.Text.Replace("_", ".");
                resource = (current.ArgumentList.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax)?.Token.ValueText;
            }

            if (method.StartsWith("PublishAsAzureContainerApp"))
                isDeployable = true;

            current = access.Expression as InvocationExpressionSyntax;
        }
        else break;
    }

    return (resource, project, isDeployable);
}

static string FindRepoRoot(string path)
{
    for (var dir = new DirectoryInfo(path); dir is not null; dir = dir.Parent)
        if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            return dir.FullName;
    return path;
}

static HashSet<string> GetProjectDependencies(string csproj, HashSet<string>? visited = null)
{
    visited ??= [];
    var deps = new HashSet<string>();

    if (!File.Exists(csproj) || !visited.Add(csproj)) return deps;

    foreach (var refPath in XDocument.Load(csproj).Descendants("ProjectReference")
        .Select(e => e.Attribute("Include")?.Value).OfType<string>())
    {
        var fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, refPath));
        var name = Path.GetFileNameWithoutExtension(fullPath);

        if (name.Contains("Generators") || name.Contains("Aspire")) continue;

        deps.Add(name);
        deps.UnionWith(GetProjectDependencies(fullPath, visited));
    }

    return deps;
}

// --- Types ---

record Resource(string Project, List<string> Dependencies);
record Output(Dictionary<string, Resource> Resources, Dictionary<string, List<string>> ProjectToResources);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Output))]
partial class JsonCtx : JsonSerializerContext;
