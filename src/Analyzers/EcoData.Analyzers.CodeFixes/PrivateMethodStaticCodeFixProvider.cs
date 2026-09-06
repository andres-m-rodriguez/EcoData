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
/// Fixes ECO004 by adding the static modifier. The fix is offered only when the body touches no
/// instance member of the containing type, since anything else needs a parameter the fix cannot
/// invent.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrivateMethodStaticCodeFixProvider))]
[Shared]
public sealed class PrivateMethodStaticCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [PrivateMethodStaticAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(context.Span);
        var declaration = node?.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (declaration is null)
            return;

        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (model is null || UsesInstanceState(declaration, model, context.CancellationToken))
            return;

        var codeAction = CodeAction.Create(
            "Make static",
            cancellationToken => AddStaticAsync(context.Document, declaration, cancellationToken),
            equivalenceKey: "MakePrivateMethodStatic");
        context.RegisterCodeFix(codeAction, context.Diagnostics[0]);
    }

    private static bool UsesInstanceState(MethodDeclarationSyntax declaration, SemanticModel model, CancellationToken cancellationToken)
    {
        var body = (SyntaxNode?)declaration.Body ?? declaration.ExpressionBody;
        if (body is null)
            return false;

        foreach (var node in body.DescendantNodes())
        {
            if (node is ThisExpressionSyntax or BaseExpressionSyntax)
                return true;

            if (node is not SimpleNameSyntax name)
                continue;

            // A name on the right of a dot is reached through the left operand, and nameof needs
            // no instance. Everything else is an implicit this access if it binds to an instance member.
            if (name.Parent is MemberAccessExpressionSyntax { Name: var accessed } && accessed == name)
                continue;

            if (name.Ancestors().OfType<InvocationExpressionSyntax>().Any(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" }))
                continue;

            var symbolInfo = model.GetSymbolInfo(name, cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (symbol is IFieldSymbol or IPropertySymbol or IMethodSymbol or IEventSymbol && !symbol.IsStatic)
                return true;

            // A primary constructor parameter is captured state, and a static member cannot read it.
            if (symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } })
                return true;
        }

        return false;
    }

    private static async Task<Document> AddStaticAsync(Document document, MethodDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var staticKeyword = SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        var modifiers = declaration.Modifiers;

        MethodDeclarationSyntax updated;
        var privateIndex = modifiers.IndexOf(SyntaxKind.PrivateKeyword);
        if (privateIndex >= 0)
        {
            updated = declaration.WithModifiers(modifiers.Insert(privateIndex + 1, staticKeyword));
        }
        else
        {
            // Implicitly private: static goes first and takes over the indentation of whatever led before.
            var first = modifiers.Count > 0 ? modifiers[0] : declaration.ReturnType.GetFirstToken();
            var leading = first.LeadingTrivia;
            updated = declaration.ReplaceToken(first, first.WithLeadingTrivia());
            updated = updated.WithModifiers(updated.Modifiers.Insert(0, staticKeyword.WithLeadingTrivia(leading)));
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root!.ReplaceNode(declaration, updated);
        return document.WithSyntaxRoot(newRoot);
    }
}
