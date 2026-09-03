using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO001: an if or else body consisting of a single simple statement must not use braces.
/// Only expression, return, throw, break, continue, and yield statements are flagged: declarations,
/// labels, and local functions are illegal as embedded statements, and a nested if could rebind a
/// dangling else if its braces were removed. Blocks whose braces carry comments, preprocessor
/// directives, or any other significant trivia are skipped so the code fix never destroys or
/// unbalances them.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleStatementIfBracesAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Single-statement if should not use braces",
        "Remove the braces from this single-statement '{0}'",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: an if or else body consisting of a single simple statement is written without braces.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIf, SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeElse, SyntaxKind.ElseClause);
    }

    private static void AnalyzeIf(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;
        if (!IsReducibleBlock(ifStatement.Statement, out var block))
            return;

        var location = block.OpenBraceToken.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location, "if");
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeElse(SyntaxNodeAnalysisContext context)
    {
        var elseClause = (ElseClauseSyntax)context.Node;
        if (!IsReducibleBlock(elseClause.Statement, out var block))
            return;

        var location = block.OpenBraceToken.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location, "else");
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsReducibleBlock(StatementSyntax statement, out BlockSyntax block)
    {
        block = null!;
        if (statement is not BlockSyntax candidate || candidate.Statements.Count != 1)
            return false;

        if (candidate.Statements[0] is not (ExpressionStatementSyntax
            or ReturnStatementSyntax
            or ThrowStatementSyntax
            or BreakStatementSyntax
            or ContinueStatementSyntax
            or YieldStatementSyntax))
            return false;

        if (HasSignificantTrivia(candidate.OpenBraceToken) || HasSignificantTrivia(candidate.CloseBraceToken))
            return false;

        block = candidate;
        return true;
    }

    private static bool HasSignificantTrivia(SyntaxToken token) =>
        token.LeadingTrivia.Any(IsSignificant) || token.TrailingTrivia.Any(IsSignificant);

    private static bool IsSignificant(SyntaxTrivia trivia) =>
        !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia);
}
