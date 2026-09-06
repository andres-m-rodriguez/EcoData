using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO005: a private method with exactly one call site is inlined into that caller. References are
/// counted across every declaration of the containing type, nested types included, and the single
/// reference must be a direct invocation. Methods that may be reached some other way (attributed,
/// partial, explicit interface implementations) and methods whose signature cannot be inlined
/// (generic, ref, out, in or params parameters) are left alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleUsePrivateMethodAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Private method with one call site should be inlined",
        "Inline private method '{0}' into its only caller",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a private method that is called from exactly one place is inlined into that caller, so the logic is read where it runs and no one-off helper hides it.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        // Generated partial declarations still count as callers; only reports inside them are dropped.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext =>
        {
            // A nested type's members run under the nested type's own symbol start, not the
            // container's, so the tally is shared. The container's symbol end still waits for
            // every member of every nested type, so the count is complete when it reports.
            var tallies = new ConcurrentDictionary<IMethodSymbol, ReferenceTally>(SymbolEqualityComparer.Default);
            startContext.RegisterSymbolStartAction(symbolContext =>
            {
                symbolContext.RegisterSyntaxNodeAction(nodeContext => CountReference(nodeContext, tallies), SyntaxKind.IdentifierName);
                symbolContext.RegisterSymbolEndAction(endContext => Report(endContext, tallies));
            }, SymbolKind.NamedType);
        });
    }

    private static void CountReference(SyntaxNodeAnalysisContext context, ConcurrentDictionary<IMethodSymbol, ReferenceTally> tallies)
    {
        var identifier = (IdentifierNameSyntax)context.Node;
        if (identifier.IsVar || identifier.Parent is NameColonSyntax or NameEqualsSyntax)
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken);
        if (symbolInfo.Symbol is IMethodSymbol symbol)
        {
            var method = (symbol.ReducedFrom ?? symbol).OriginalDefinition;
            if (!IsCandidate(method))
                return;

            var invocation = InvocationOf(identifier);
            var tally = tallies.GetOrAdd(method, _ => new ReferenceTally());
            lock (tally)
            {
                tally.Count++;
                tally.Invoked = invocation is not null;
            }

            return;
        }

        // An unresolved reference (a method group between overloads, a call that failed overload
        // resolution) still pins the method in place, but is never the one call site to inline.
        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (candidate is not IMethodSymbol candidateMethod)
                continue;

            var method = (candidateMethod.ReducedFrom ?? candidateMethod).OriginalDefinition;
            if (!IsCandidate(method))
                continue;

            var tally = tallies.GetOrAdd(method, _ => new ReferenceTally());
            lock (tally)
            {
                tally.Count++;
                tally.Invoked = false;
            }
        }
    }

    private static bool IsCandidate(IMethodSymbol method)
    {
        if (method.MethodKind != MethodKind.Ordinary || method.DeclaredAccessibility != Accessibility.Private)
            return false;

        if (method.IsImplicitlyDeclared || method.IsGenericMethod || method.DeclaringSyntaxReferences.Length != 1)
            return false;

        if (method.IsPartialDefinition || method.PartialDefinitionPart is not null || method.PartialImplementationPart is not null)
            return false;

        if (!method.ExplicitInterfaceImplementations.IsEmpty || !method.GetAttributes().IsEmpty)
            return false;

        return method.Parameters.All(parameter => parameter.RefKind == RefKind.None && !parameter.IsParams);
    }

    public static InvocationExpressionSyntax? InvocationOf(IdentifierNameSyntax identifier)
    {
        SyntaxNode reference = identifier;
        if (identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier)
            reference = access;

        if (reference.Parent is InvocationExpressionSyntax invocation && invocation.Expression == reference)
            return invocation;

        return null;
    }

    private static void Report(SymbolAnalysisContext context, ConcurrentDictionary<IMethodSymbol, ReferenceTally> tallies)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method || !tallies.TryRemove(method, out var tally))
                continue;

            if (tally.Count != 1 || !tally.Invoked)
                continue;

            var location = method.Locations[0];
            var diagnostic = Diagnostic.Create(Rule, location, method.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private sealed class ReferenceTally
    {
        public int Count;
        public bool Invoked;
    }
}
