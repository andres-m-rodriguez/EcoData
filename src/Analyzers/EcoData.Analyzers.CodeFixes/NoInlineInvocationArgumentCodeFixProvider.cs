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
using Microsoft.CodeAnalysis.Editing;

namespace EcoData.Analyzers;

/// <summary>
/// Fixes ECO002 by assigning the flagged call (including a wrapping await) to a new local variable
/// declared immediately before the enclosing statement, then passing the variable instead. The
/// variable name is derived from the invoked method name, with a numeric suffix on collision.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoInlineInvocationArgumentCodeFixProvider))]
[Shared]
public sealed class NoInlineInvocationArgumentCodeFixProvider : CodeFixProvider
{
    private static readonly ImmutableArray<string> NamePrefixes =
        ImmutableArray.Create("Get", "Create", "Build", "Read", "Compute");

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(NoInlineInvocationArgumentAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (node is not InvocationExpressionSyntax invocation)
            return;

        var codeAction = CodeAction.Create(
            "Extract argument to local variable",
            cancellationToken => ExtractToLocalAsync(context.Document, invocation, cancellationToken),
            equivalenceKey: "ExtractArgumentToLocal");
        context.RegisterCodeFix(codeAction, context.Diagnostics[0]);
    }

    private static async Task<Document> ExtractToLocalAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        ExpressionSyntax target = invocation;
        if (invocation.Parent is AwaitExpressionSyntax awaitExpression)
            target = awaitExpression;

        var statement = target.FirstAncestorOrSelf<StatementSyntax>();
        if (statement is null || statement.Parent is not BlockSyntax)
            return document;

        var name = DeriveName(invocation, statement);

        var expression = target.WithoutTrivia();
        var initializer = SyntaxFactory.EqualsValueClause(expression);
        var declarator = SyntaxFactory.VariableDeclarator(name);
        declarator = declarator.WithInitializer(initializer);
        var declaratorList = SyntaxFactory.SingletonSeparatedList(declarator);
        var varType = SyntaxFactory.IdentifierName("var");
        var declaration = SyntaxFactory.VariableDeclaration(varType, declaratorList);
        var local = SyntaxFactory.LocalDeclarationStatement(declaration);

        var leadingTrivia = statement.GetLeadingTrivia();
        var indentation = leadingTrivia.LastOrDefault(IsWhitespace);
        if (indentation.IsKind(SyntaxKind.WhitespaceTrivia))
            local = local.WithLeadingTrivia(indentation);

        var trailingTrivia = statement.GetTrailingTrivia();
        var newline = trailingTrivia.LastOrDefault(IsEndOfLine);
        if (!newline.IsKind(SyntaxKind.EndOfLineTrivia))
            newline = SyntaxFactory.CarriageReturnLineFeed;
        local = local.WithTrailingTrivia(newline);

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.InsertBefore(statement, local);
        var reference = SyntaxFactory.IdentifierName(name);
        editor.ReplaceNode(target, reference);
        return editor.GetChangedDocument();
    }

    private static string DeriveName(InvocationExpressionSyntax invocation, StatementSyntax statement)
    {
        var methodName = NoInlineInvocationArgumentAnalyzer.GetInvokedName(invocation);
        var candidate = methodName;
        if (candidate.EndsWith("Async", StringComparison.Ordinal))
            candidate = candidate.Substring(0, candidate.Length - "Async".Length);

        foreach (var prefix in NamePrefixes)
        {
            if (candidate.Length > prefix.Length && candidate.StartsWith(prefix, StringComparison.Ordinal))
            {
                candidate = candidate.Substring(prefix.Length);
                break;
            }
        }

        if (candidate.Length == 0 || !char.IsLetter(candidate[0]))
            candidate = "result";

        candidate = char.ToLowerInvariant(candidate[0]) + candidate.Substring(1);
        var keywordKind = SyntaxFacts.GetKeywordKind(candidate);
        if (keywordKind != SyntaxKind.None)
            candidate += "Value";

        var scope = statement.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? (SyntaxNode)statement;
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in scope.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.IdentifierToken))
                usedNames.Add(token.Text);
        }

        var name = candidate;
        var suffix = 2;
        while (usedNames.Contains(name))
        {
            name = candidate + suffix;
            suffix++;
        }

        return name;
    }

    private static bool IsWhitespace(SyntaxTrivia trivia) => trivia.IsKind(SyntaxKind.WhitespaceTrivia);

    private static bool IsEndOfLine(SyntaxTrivia trivia) => trivia.IsKind(SyntaxKind.EndOfLineTrivia);
}
