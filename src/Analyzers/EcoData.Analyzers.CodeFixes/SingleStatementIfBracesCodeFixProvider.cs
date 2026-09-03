using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EcoData.Analyzers;

/// <summary>
/// Fixes ECO001 by replacing the braced block with its single statement. The statement keeps its
/// own trivia, so it stays on the line and at the indentation it already had inside the braces.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingleStatementIfBracesCodeFixProvider))]
[Shared]
public sealed class SingleStatementIfBracesCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [SingleStatementIfBracesAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindToken(context.Span.Start).Parent is not BlockSyntax block)
            return;

        var codeAction = CodeAction.Create(
            "Remove braces",
            cancellationToken => RemoveBracesAsync(context.Document, block, cancellationToken),
            equivalenceKey: "RemoveBraces");
        context.RegisterCodeFix(codeAction, context.Diagnostics[0]);
    }

    private static async Task<Document> RemoveBracesAsync(
        Document document,
        BlockSyntax block,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root!.ReplaceNode(block, block.Statements[0]);
        return document.WithSyntaxRoot(newRoot);
    }
}
