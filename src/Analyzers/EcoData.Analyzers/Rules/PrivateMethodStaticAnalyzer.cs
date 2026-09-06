using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO004: a private method must be static. Everything the method works on then arrives through
/// its parameters, so the signature states its intent and the caller keeps the instance state.
/// Only ordinary methods are checked: constructors, accessors, operators, local functions,
/// explicit interface implementations and partial methods are left alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrivateMethodStaticAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO004";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Private method should be static",
        "Make private method '{0}' static and pass what it needs as parameters",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a private method is static, so everything it works on arrives through its parameters and the signature states its intent.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.MethodKind != MethodKind.Ordinary || method.IsStatic || method.IsImplicitlyDeclared)
            return;

        if (method.DeclaredAccessibility != Accessibility.Private)
            return;

        if (method.IsPartialDefinition || method.PartialDefinitionPart is not null || method.PartialImplementationPart is not null)
            return;

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax(context.CancellationToken);
            if (syntax is not MethodDeclarationSyntax declaration)
                continue;

            var location = declaration.Identifier.GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location, method.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
