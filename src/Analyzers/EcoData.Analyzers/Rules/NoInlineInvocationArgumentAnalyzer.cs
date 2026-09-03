using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO002: the result of a method call must be assigned to a local variable before being passed as
/// an argument to another method call. Flagged only where hoisting to a local is provably safe: the
/// call sits directly in an argument list (optionally under await) with no ref or out modifier, no
/// observable work completes earlier in the same statement, and the walk up to the nearest
/// block-level statement crosses no construct that would make the hoisted call evaluate at a
/// different time, frequency, or scope (lambdas, local functions, conditional access, ternaries,
/// short-circuit operators, switch and query expressions, case when clauses, catch clauses, object
/// and collection initializers, while/do/for loop headers). nameof is a constant, not a call, and
/// is never flagged.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoInlineInvocationArgumentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Method call result should not be passed inline as an argument",
        "Assign the result of '{0}' to a local variable and pass the variable instead",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a call whose result feeds another call is first assigned to a local variable, keeping argument lists flat and debuggable.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!IsDirectInvocationArgument(invocation) || IsNameOf(invocation))
            return;

        var statement = FindInsertionPoint(invocation);
        if (statement is null)
            return;

        var target = GetHoistTarget(invocation);
        if (!NoObservableWorkBefore(statement, target))
            return;

        var location = invocation.GetLocation();
        var invokedName = GetInvokedName(invocation);
        var diagnostic = Diagnostic.Create(Rule, location, invokedName);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>The node that would be hoisted: the invocation, or the await expression wrapping it.</summary>
    public static ExpressionSyntax GetHoistTarget(InvocationExpressionSyntax invocation) =>
        invocation.Parent is AwaitExpressionSyntax awaitExpression ? awaitExpression : invocation;

    /// <summary>True when the call (or the await wrapping it) is directly a by-value argument of another call.</summary>
    public static bool IsDirectInvocationArgument(InvocationExpressionSyntax invocation)
    {
        var target = GetHoistTarget(invocation);
        return target.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax } } argument
            && argument.RefKindKeyword.IsKind(SyntaxKind.None);
    }

    /// <summary>True for nameof, which parses as an invocation but is a compile-time constant.</summary>
    public static bool IsNameOf(InvocationExpressionSyntax invocation) =>
        invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" };

    /// <summary>
    /// Returns the block-level statement a local could be declared before, or null when no such
    /// statement exists without changing semantics. Rejects every construct on the way up that would
    /// make the hoisted call evaluate at a different time or frequency, or lose access to names in
    /// scope: lambdas, local functions, conditional access, ternaries, short-circuit operators,
    /// switch and query expressions, case when clauses, catch clauses, object, collection, and with
    /// initializers (the constructor would otherwise run after the hoisted call), and while, do, and
    /// for loops reached through any part of their header (their initializer sections included,
    /// conservatively; foreach sources evaluate once and stay flagged).
    /// </summary>
    public static StatementSyntax? FindInsertionPoint(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is AnonymousFunctionExpressionSyntax
                or LocalFunctionStatementSyntax
                or ConditionalAccessExpressionSyntax
                or ConditionalExpressionSyntax
                or SwitchExpressionSyntax
                or QueryExpressionSyntax
                or CatchClauseSyntax
                or WhenClauseSyntax
                or InitializerExpressionSyntax)
                return null;

            if (current is BinaryExpressionSyntax binary && IsShortCircuit(binary))
                return null;

            if (current is WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax)
                return null;

            if (current is StatementSyntax statement)
                return statement.Parent is BlockSyntax ? statement : null;
        }

        return null;
    }

    /// <summary>
    /// True when nothing observable (a call, object creation, assignment, increment, decrement, or
    /// await) completes earlier in the statement than the hoist target, so declaring the local above
    /// the statement cannot reorder work. Member and element accesses are treated as side-effect
    /// free, a deliberate trade-off shared with built-in refactorings. Work inside earlier lambdas
    /// counts even though it would not run before the target, a conservative false negative.
    /// </summary>
    public static bool NoObservableWorkBefore(StatementSyntax statement, ExpressionSyntax target)
    {
        foreach (var node in statement.DescendantNodes())
        {
            if (node.Span.End > target.SpanStart)
                continue;

            if (IsObservableWork(node))
                return false;
        }

        return true;
    }

    /// <summary>The simple name being invoked, for diagnostics and variable naming.</summary>
    public static string GetInvokedName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.Text,
            SimpleNameSyntax simpleName => simpleName.Identifier.Text,
            _ => invocation.Expression.ToString(),
        };

    private static bool IsShortCircuit(BinaryExpressionSyntax binary) =>
        binary.IsKind(SyntaxKind.LogicalAndExpression)
        || binary.IsKind(SyntaxKind.LogicalOrExpression)
        || binary.IsKind(SyntaxKind.CoalesceExpression);

    private static bool IsObservableWork(SyntaxNode node) =>
        node switch
        {
            InvocationExpressionSyntax invocation => !IsNameOf(invocation),
            BaseObjectCreationExpressionSyntax => true,
            AssignmentExpressionSyntax => true,
            AwaitExpressionSyntax => true,
            PrefixUnaryExpressionSyntax prefix =>
                prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression),
            PostfixUnaryExpressionSyntax postfix =>
                postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression),
            _ => false,
        };
}
