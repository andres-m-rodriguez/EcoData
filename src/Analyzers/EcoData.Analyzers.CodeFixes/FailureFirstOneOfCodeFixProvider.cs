using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EcoData.Analyzers;

/// <summary>
/// Fixes ECO006 by negating the condition, moving the failure branch into the if, and emitting the
/// old success statements after it at the if's own indentation. Without an else clause the failure
/// branch becomes a bare return, so the fix is only offered where that compiles and exits the
/// member: a void, async Task, or async ValueTask method, local function, or lambda, with the if
/// closing every block up to the member body.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FailureFirstOneOfCodeFixProvider))]
[Shared]
public sealed class FailureFirstOneOfCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [FailureFirstOneOfAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (node?.FirstAncestorOrSelf<IfStatementSyntax>() is not { } ifStatement || ifStatement.Condition.Span != context.Span)
            return;

        if (ifStatement.Parent is not (BlockSyntax or SwitchSectionSyntax))
            return;

        if (ifStatement.Else is null)
        {
            var canReturn = await CanReturnBareAsync(context.Document, ifStatement, context.CancellationToken).ConfigureAwait(false);
            if (!canReturn)
                return;
        }

        var codeAction = CodeAction.Create(
            "Handle failure first",
            cancellationToken => HandleFailureFirstAsync(context.Document, ifStatement, cancellationToken),
            equivalenceKey: "FailureFirstOneOf");
        context.RegisterCodeFix(codeAction, context.Diagnostics[0]);
    }

    private static async Task<bool> CanReturnBareAsync(
        Document document,
        IfStatementSyntax ifStatement,
        CancellationToken cancellationToken)
    {
        var owner = TailOwner(ifStatement);
        if (owner is null)
            return false;

        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (model is null)
            return false;

        var symbol = owner is AnonymousFunctionExpressionSyntax
            ? model.GetSymbolInfo(owner, cancellationToken).Symbol
            : model.GetDeclaredSymbol(owner, cancellationToken);
        if (symbol is not IMethodSymbol method)
            return false;

        if (method.ReturnsVoid)
            return true;

        var returnType = method.ReturnType.ToDisplayString();
        return method.IsAsync && returnType is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask";
    }

    // A bare return exits the member, so the if must close every block up to the member body and
    // sit under nothing a return would leave early: no loop, switch section, or finally clause.
    private static SyntaxNode? TailOwner(IfStatementSyntax ifStatement)
    {
        SyntaxNode current = ifStatement;
        while (true)
        {
            if (current.Parent is not BlockSyntax block || block.Statements.Last() != current)
                return null;

            switch (block.Parent)
            {
                case MethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax:
                    return block.Parent;
                case ElseClauseSyntax or CatchClauseSyntax:
                    current = block.Parent.Parent!;
                    break;
                case IfStatementSyntax or BlockSyntax or TryStatementSyntax or UsingStatementSyntax
                    or LockStatementSyntax or CheckedStatementSyntax or UnsafeStatementSyntax:
                    current = block.Parent;
                    break;
                default:
                    return null;
            }
        }
    }

    private static async Task<Document> HandleFailureFirstAsync(
        Document document,
        IfStatementSyntax ifStatement,
        CancellationToken cancellationToken)
    {
        var newline = ifStatement.DescendantTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        if (!newline.IsKind(SyntaxKind.EndOfLineTrivia))
            newline = SyntaxFactory.CarriageReturnLineFeed;

        var happy = ifStatement.Statement is BlockSyntax block
            ? block.Statements
            : SyntaxFactory.SingletonList(ifStatement.Statement);
        var ifIndent = Indentation(ifStatement.GetFirstToken());
        var happyIndent = happy.Count > 0 ? Indentation(happy[0].GetFirstToken()) : ifIndent;
        var bodyIndent = happyIndent.Length > ifIndent.Length ? happyIndent : ifIndent + "    ";

        var operand = FailureFirstOneOfAnalyzer.Unwrap(ifStatement.Condition).WithoutTrivia();
        if (operand is not (MemberAccessExpressionSyntax or InvocationExpressionSyntax))
            operand = SyntaxFactory.ParenthesizedExpression(operand);
        var negated = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, operand)
            .WithTriviaFrom(ifStatement.Condition);

        var bodyWhitespace = SyntaxFactory.Whitespace(bodyIndent);
        IfStatementSyntax newIf;
        if (ifStatement.Else is { } elseClause)
        {
            var failure = elseClause.Statement;
            var onOwnLine = ifStatement.CloseParenToken.TrailingTrivia.Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
            var failureToken = failure.GetFirstToken();
            if (failure is not BlockSyntax && onOwnLine && Indentation(failureToken).Length == 0)
                failure = failure.WithLeadingTrivia(bodyWhitespace);

            newIf = ifStatement.WithCondition(negated).WithStatement(failure).WithElse(null);
        }
        else
        {
            var returnStatement = SyntaxFactory.ReturnStatement()
                .WithLeadingTrivia(bodyWhitespace)
                .WithTrailingTrivia(newline);
            var closeParen = ifStatement.CloseParenToken.WithTrailingTrivia(newline);
            newIf = ifStatement.WithCondition(negated).WithCloseParenToken(closeParen).WithStatement(returnStatement);
        }

        var guard = EndLine(newIf, newline);
        var replacement = new List<SyntaxNode> { guard };
        foreach (var statement in happy)
        {
            var dedented = statement.ReplaceTokens(
                statement.DescendantTokens(),
                (token, _) => token.WithLeadingTrivia(Dedent(token.LeadingTrivia, happyIndent, ifIndent)));
            replacement.Add(dedented);
        }

        if (happy.Count > 0)
        {
            var first = replacement[1];
            var firstToken = first.GetFirstToken();
            var leading = firstToken.LeadingTrivia;
            if (leading.Count == 0 || !leading[0].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                var prefix = leading.Count > 0 && leading[0].IsKind(SyntaxKind.WhitespaceTrivia)
                    ? new[] { newline }
                    : new[] { newline, SyntaxFactory.Whitespace(ifIndent) };
                leading = leading.InsertRange(0, prefix);
            }

            var indentedToken = firstToken.WithLeadingTrivia(leading);
            replacement[1] = first.ReplaceToken(firstToken, indentedToken);
            replacement[replacement.Count - 1] = EndLine(replacement[replacement.Count - 1], newline);
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root!.ReplaceNode(ifStatement, replacement);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string Indentation(SyntaxToken token)
    {
        var leading = token.LeadingTrivia;
        if (leading.Count == 0 || !leading[leading.Count - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            return "";

        return leading[leading.Count - 1].ToString();
    }

    private static SyntaxTriviaList Dedent(SyntaxTriviaList trivia, string fromIndent, string toIndent)
    {
        if (fromIndent == toIndent)
            return trivia;

        var result = new List<SyntaxTrivia>(trivia.Count);
        for (var i = 0; i < trivia.Count; i++)
        {
            var current = trivia[i];
            var startsLine = i == 0 || trivia[i - 1].IsKind(SyntaxKind.EndOfLineTrivia);
            var text = current.ToString();
            if (startsLine && current.IsKind(SyntaxKind.WhitespaceTrivia) && text.StartsWith(fromIndent, StringComparison.Ordinal))
                current = SyntaxFactory.Whitespace(toIndent + text.Substring(fromIndent.Length));

            result.Add(current);
        }

        return SyntaxFactory.TriviaList(result);
    }

    private static SyntaxNode EndLine(SyntaxNode node, SyntaxTrivia newline)
    {
        var lastToken = node.GetLastToken();
        if (lastToken.TrailingTrivia.Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
            return node;

        var kept = lastToken.TrailingTrivia.Where(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia));
        var trailing = SyntaxFactory.TriviaList(kept).Add(newline);
        var endedToken = lastToken.WithTrailingTrivia(trailing);
        return node.ReplaceToken(lastToken, endedToken);
    }
}
