using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO008: a record in a Contracts or Dtos namespace carries only data. Every instance member
/// that carries logic is flagged: ordinary methods, explicit interface implementations, indexers,
/// and properties with an expression body or a non-auto accessor. Positional parameters,
/// auto-properties, fields, constructors, operators, Deconstruct and static members are not logic.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataRecordStaysDataAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO008";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Contract record should carry only data",
        "Move '{0}' out of record '{1}': contract records carry data, the consumer computes what it needs",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a record in a Contracts or Dtos namespace moves between components as plain data. Derived values such as IsEmpty, ActiveCount or DisplayName are computed by the component that needs them, not baked onto the record.");

    private static readonly ImmutableHashSet<string> ContractNamespaceSegments =
        ImmutableHashSet.Create("Contracts", "Contract", "Dto", "Dtos");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeRecord, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeRecord(SyntaxNodeAnalysisContext context)
    {
        var record = (RecordDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(record, context.CancellationToken);
        if (symbol is null || !IsContractNamespace(symbol.ContainingNamespace))
            return;

        var recordName = record.Identifier.ValueText;
        foreach (var member in record.Members)
        {
            if (member.Modifiers.Any(SyntaxKind.StaticKeyword))
                continue;

            var identifier = GetLogicMemberIdentifier(member);
            if (identifier is null)
                continue;

            var location = identifier.Value.GetLocation();
            var diagnostic = Diagnostic.Create(Rule, location, identifier.Value.ValueText, recordName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsContractNamespace(INamespaceSymbol? ns)
    {
        while (ns is not null && !ns.IsGlobalNamespace)
        {
            if (ContractNamespaceSegments.Contains(ns.Name))
                return true;

            ns = ns.ContainingNamespace;
        }

        return false;
    }

    // Nested records are visited by their own callback, so nested type declarations are skipped here.
    private static SyntaxToken? GetLogicMemberIdentifier(MemberDeclarationSyntax member)
    {
        switch (member)
        {
            case MethodDeclarationSyntax method when method.Identifier.ValueText != "Deconstruct":
                return method.Identifier;
            case IndexerDeclarationSyntax indexer:
                return indexer.ThisKeyword;
            case PropertyDeclarationSyntax property when HasLogic(property):
                return property.Identifier;
            default:
                return null;
        }
    }

    private static bool HasLogic(PropertyDeclarationSyntax property)
    {
        if (property.ExpressionBody is not null)
            return true;

        if (property.AccessorList is null)
            return false;

        return property.AccessorList.Accessors.Any(accessor => accessor.Body is not null || accessor.ExpressionBody is not null);
    }
}
