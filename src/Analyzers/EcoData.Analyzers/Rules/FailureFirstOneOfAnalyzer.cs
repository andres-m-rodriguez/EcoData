using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO006: a OneOf result is checked for failure first with an early return, so the happy path is
/// not nested inside the success branch. An if whose condition is a success check (IsT0 or a
/// TryPickT0 call) is flagged when its else clause ends in a jump, or when it has no else, closes
/// its block, and nests two or more statements. Negated conditions already read failure-first, a
/// then body that ends in a jump is an early-out on success, and else-if chains are a different
/// shape; none of those are flagged.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FailureFirstOneOfAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO006";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "OneOf failure should be handled first",
        "Return on the failure branch of '{0}' first, then continue with the happy path un-nested",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a OneOf result is checked for failure first with an early return, so the happy path is not nested inside the success branch.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIf, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIf(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;
        if (ifStatement.Parent is ElseClauseSyntax || ifStatement.Else?.Statement is IfStatementSyntax)
            return;

        if (!IsSuccessCheck(ifStatement.Condition) || EndsInJump(ifStatement.Statement))
            return;

        var handledInElse = ifStatement.Else is { } elseClause && EndsInJump(elseClause.Statement);
        var droppedSilently = ifStatement.Else is null
            && ifStatement.Statement is BlockSyntax { Statements.Count: >= 2 }
            && ifStatement.Parent is BlockSyntax parent
            && parent.Statements.Last() == ifStatement;
        if (!handledInElse && !droppedSilently)
            return;

        var location = ifStatement.Condition.GetLocation();
        var condition = ifStatement.Condition.ToString();
        var diagnostic = Diagnostic.Create(Rule, location, condition);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>True when the condition, parentheses aside, reads IsT0 or calls TryPickT0.</summary>
    public static bool IsSuccessCheck(ExpressionSyntax condition)
    {
        var expression = Unwrap(condition);
        return expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text == "IsT0",
            InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "TryPickT0" } } => true,
            InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "TryPickT0" } } => true,
            _ => false,
        };
    }

    public static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }

    private static bool EndsInJump(StatementSyntax statement)
    {
        var last = statement is BlockSyntax block ? block.Statements.LastOrDefault() : statement;
        return last is ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax;
    }
}
