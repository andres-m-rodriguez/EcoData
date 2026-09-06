using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO007: a type name must not end in a generic suffix such as Service, Manager, Helper, Utils or
/// Common. Classes, structs, records and interfaces are judged by their name alone, so an
/// interface's leading I and a generic arity change nothing; enums and delegates are skipped. Base
/// types are deliberately not consulted: a subclass of a framework type that carries a suffix is
/// still named for the responsibility it owns. Partial types are reported on every declaration.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BannedTypeNameSuffixAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO007";

    private static readonly ImmutableArray<string> Suffixes =
        ["Service", "Manager", "Helper", "Helpers", "Utils", "Utilities", "Common"];

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Type name should not end in a generic suffix",
        "Rename '{0}': the suffix '{1}' says nothing, name the type for the responsibility it owns",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a type is named for the responsibility it owns. Suffixes such as Service, Manager, Helper, Utils and Common describe nothing and collect unrelated members.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(symbolContext => AnalyzeNamedType(symbolContext), SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.IsImplicitlyDeclared)
            return;

        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
            return;

        var suffix = Suffixes.FirstOrDefault(candidate => type.Name.EndsWith(candidate, StringComparison.Ordinal));
        if (suffix is null)
            return;

        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax(context.CancellationToken);
            if (syntax is not BaseTypeDeclarationSyntax declaration)
                continue;

            var location = declaration.Identifier.GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location, type.Name, suffix);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
