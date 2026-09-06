using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO011: a component declares its services with <c>@inject</c> in its <c>.razor</c> file, not
/// with <c>[Inject]</c> in a code-behind. The one place <c>[Inject]</c> is allowed is an abstract
/// base class, which has no <c>.razor</c> file of its own. The attribute is matched by symbol so a
/// same-named type from another namespace is ignored.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoInjectAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO011";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Component services should be declared with @inject",
        "Declare '{0}' with @inject in the .razor file instead of [Inject]",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a component's dependencies are visible at the top of its .razor file. [Inject] stays only on abstract base classes, which have no .razor file to hold the directive.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext => RegisterAttributeAnalysis(startContext));
    }

    private static void RegisterAttributeAnalysis(CompilationStartAnalysisContext context)
    {
        var inject = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.InjectAttribute");
        if (inject is null)
            return;

        context.RegisterSyntaxNodeAction(nodeContext => AnalyzeAttribute(nodeContext, inject), SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, INamedTypeSymbol inject)
    {
        var attribute = (AttributeSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol constructor)
            return;

        if (!SymbolEqualityComparer.Default.Equals(constructor.ContainingType, inject))
            return;

        if (attribute.Parent?.Parent is not MemberDeclarationSyntax member)
            return;

        var declared = context.SemanticModel.GetDeclaredSymbol(member, context.CancellationToken);
        if (declared?.ContainingType is { IsAbstract: true })
            return;

        // The list carries the brackets, so the whole [Inject] is underlined.
        var location = attribute.Parent.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location, declared?.Name ?? "this member");
        context.ReportDiagnostic(diagnostic);
    }
}
