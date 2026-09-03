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
/// Fixes ECO003 by wrapping the method group in a lambda that forwards its parameters. The
/// parameters take the target method's own names, with a numeric suffix where a name is already
/// taken in the enclosing member.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NoMethodGroupArgumentCodeFixProvider))]
[Shared]
public sealed class NoMethodGroupArgumentCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [NoMethodGroupArgumentAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (node is not ExpressionSyntax { Parent: ArgumentSyntax } expression)
            return;

        var codeAction = CodeAction.Create(
            "Replace with lambda",
            cancellationToken => ToLambdaAsync(context.Document, expression, cancellationToken),
            equivalenceKey: "ReplaceMethodGroupWithLambda");
        context.RegisterCodeFix(codeAction, context.Diagnostics[0]);
    }

    private static async Task<Document> ToLambdaAsync(
        Document document,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (model is null)
            return document;

        var typeInfo = model.GetTypeInfo(expression, cancellationToken);
        if (typeInfo.ConvertedType is not INamedTypeSymbol { DelegateInvokeMethod: { } invoke })
            return document;

        var symbolInfo = model.GetSymbolInfo(expression, cancellationToken);
        var method = (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;
        var names = ParameterNames(expression, invoke, method);

        var arguments = names.Select(name => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(name)));
        var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
        var body = SyntaxFactory.InvocationExpression(expression.WithoutTrivia(), argumentList);

        LambdaExpressionSyntax lambda;
        if (names.Count == 1)
        {
            var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(names[0]));
            lambda = SyntaxFactory.SimpleLambdaExpression(parameter, body);
        }
        else
        {
            var parameters = names.Select(name => SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)));
            var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
            lambda = SyntaxFactory.ParenthesizedLambdaExpression(parameterList, body);
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root!.ReplaceNode(expression, lambda.WithTriviaFrom(expression));
        return document.WithSyntaxRoot(newRoot);
    }

    // The target method's parameter names read best; the delegate's own (arg1, arg2) are the
    // fallback when overload resolution left the method open or the counts disagree, as with a
    // reduced extension method.
    private static List<string> ParameterNames(ExpressionSyntax expression, IMethodSymbol invoke, IMethodSymbol? method)
    {
        var source = method is not null && method.Parameters.Length == invoke.Parameters.Length
            ? method.Parameters
            : invoke.Parameters;

        var scope = expression.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? (SyntaxNode)expression;
        var usedNames = new HashSet<string>();
        foreach (var token in scope.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.IdentifierToken))
                usedNames.Add(token.Text);
        }

        var names = new List<string>(source.Length);
        foreach (var parameter in source)
        {
            var candidate = parameter.Name;
            var name = candidate;
            var suffix = 2;
            while (usedNames.Contains(name))
            {
                name = candidate + suffix;
                suffix++;
            }

            usedNames.Add(name);
            names.Add(name);
        }

        return names;
    }
}
