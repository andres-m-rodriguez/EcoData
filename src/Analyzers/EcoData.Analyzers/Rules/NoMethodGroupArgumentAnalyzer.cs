using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO003: a method group must not be passed as an argument. The callback is written as a lambda
/// instead, so the call it makes and the values it forwards are visible at the call site. Only an
/// expression that binds to a method and converts to a delegate is flagged; nameof, lambdas,
/// delegate-typed locals and invocation results are not method groups.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoMethodGroupArgumentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO003";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Method group should not be passed as an argument",
        "Pass a lambda instead of the method group '{0}'",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a callback argument is written as a lambda, so the call it makes and the values it forwards are visible where it is passed.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeArgument, SyntaxKind.Argument);
    }

    private static void AnalyzeArgument(SyntaxNodeAnalysisContext context)
    {
        var argument = (ArgumentSyntax)context.Node;
        if (!IsMethodGroup(argument.Expression, context.SemanticModel, context.CancellationToken))
            return;

        var location = argument.Expression.GetLocation();
        var name = argument.Expression.ToString();
        var diagnostic = Diagnostic.Create(Rule, location, name);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>True when the expression names a method and is being converted to a delegate.</summary>
    public static bool IsMethodGroup(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
    {
        if (expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or GenericNameSyntax))
            return false;

        var symbolInfo = model.GetSymbolInfo(expression, cancellationToken);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        if (symbol is not IMethodSymbol)
            return false;

        var typeInfo = model.GetTypeInfo(expression, cancellationToken);
        return typeInfo.ConvertedType?.TypeKind == TypeKind.Delegate;
    }
}
