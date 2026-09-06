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
using Microsoft.CodeAnalysis.Formatting;

namespace EcoData.Analyzers;

/// <summary>
/// Fixes ECO005 by moving the method body to its single call site and deleting the method. An
/// expression body (or a lone return) replaces the invocation with the parameters substituted by
/// the arguments; a void or Task block body called as a statement is spliced in with one local per
/// parameter. The fix is only offered when the move provably keeps the meaning: no name in the body
/// may be captured or shadowed at the call site, no argument may be dropped or repeated unless it
/// is side-effect free, and no conversion may change.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingleUsePrivateMethodCodeFixProvider))]
[Shared]
public sealed class SingleUsePrivateMethodCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [SingleUsePrivateMethodAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindToken(context.Span.Start).Parent is not MethodDeclarationSyntax method)
            return;

        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (model is null)
            return;

        var symbol = model.GetDeclaredSymbol(method, context.CancellationToken);
        if (symbol is null)
            return;

        var invocation = FindCallSite(root, symbol, model, context.CancellationToken);
        if (invocation is null)
            return;

        var plan = PlanExpression(method, symbol, invocation, model, context.CancellationToken)
            ?? PlanStatements(method, symbol, invocation, model, context.CancellationToken);
        if (plan is null)
            return;

        var codeAction = CodeAction.Create(
            "Inline into caller",
            cancellationToken => InlineAsync(context.Document, method, plan, cancellationToken),
            equivalenceKey: "InlineSingleUsePrivateMethod");
        context.RegisterCodeFix(codeAction, context.Diagnostics[0]);
    }

    private static async Task<Document> InlineAsync(
        Document document,
        MethodDeclarationSyntax method,
        InlinePlan plan,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.RemoveNode(method, SyntaxRemoveOptions.KeepNoTrivia);

        if (method.Parent is TypeDeclarationSyntax type && type.Members.Count > 1 && type.Members[0] == method)
        {
            var next = type.Members[1];
            editor.ReplaceNode(next, (current, _) => WithoutLeadingBlankLines(current));
        }

        if (plan.Target is StatementSyntax statement)
        {
            editor.InsertAfter(statement, plan.Replacements);
            editor.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
        }
        else
        {
            editor.ReplaceNode(plan.Target, plan.Replacements[0]);
        }

        return editor.GetChangedDocument();
    }

    // The call site must live in this document: the type may be partial across files, and a fix
    // that edits two documents does not batch.
    private static InvocationExpressionSyntax? FindCallSite(
        SyntaxNode root,
        IMethodSymbol symbol,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        InvocationExpressionSyntax? invocation = null;
        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Identifier.ValueText != symbol.Name)
                continue;

            var symbolInfo = model.GetSymbolInfo(identifier, cancellationToken);
            if (!SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol, symbol))
                continue;

            if (invocation is not null)
                return null;

            invocation = SingleUsePrivateMethodAnalyzer.InvocationOf(identifier);
            if (invocation is null)
                return null;
        }

        if (invocation is null)
            return null;

        if (invocation.Expression is IdentifierNameSyntax)
            return invocation;

        if (invocation.Expression is not MemberAccessExpressionSyntax access)
            return null;

        if (access.Expression is ThisExpressionSyntax)
            return invocation;

        var receiver = model.GetSymbolInfo(access.Expression, cancellationToken);
        return symbol.IsStatic && receiver.Symbol is INamedTypeSymbol ? invocation : null;
    }

    private static InlinePlan? PlanExpression(
        MethodDeclarationSyntax method,
        IMethodSymbol symbol,
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        var expression = method.ExpressionBody?.Expression;
        if (expression is null && method.Body is { Statements.Count: 1 } body && body.Statements[0] is ReturnStatementSyntax { Expression: { } returned })
            expression = returned;

        if (expression is null || expression is ThrowExpressionSyntax or RefExpressionSyntax || symbol.RefKind != RefKind.None)
            return null;

        if (symbol.IsAsync)
        {
            if (expression is not AwaitExpressionSyntax { Expression: var awaited } || invocation.Parent is not AwaitExpressionSyntax)
                return null;

            expression = awaited;
            var awaitedType = model.GetTypeInfo(expression, cancellationToken);
            if (!SymbolEqualityComparer.Default.Equals(awaitedType.Type, symbol.ReturnType))
                return null;
        }
        else if (!HasIdentityConversion(expression, model, cancellationToken))
        {
            return null;
        }

        if (invocation.Parent is ExpressionStatementSyntax && !IsStatementExpression(expression))
            return null;

        if (HasNameConflict(expression, symbol, invocation, model, cancellationToken))
            return null;

        var substitutions = new Dictionary<SyntaxNode, ExpressionSyntax>();
        foreach (var parameter in symbol.Parameters)
        {
            var argument = ArgumentFor(parameter, method, invocation);
            if (argument is null || !HasIdentityConversion(argument, model, cancellationToken))
                return null;

            var occurrences = new List<IdentifierNameSyntax>();
            foreach (var identifier in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            {
                var bound = model.GetSymbolInfo(identifier, cancellationToken).Symbol;
                if (!SymbolEqualityComparer.Default.Equals(bound, parameter))
                    continue;

                if (IsWriteTarget(identifier) || IsInsideNameOf(identifier))
                    return null;

                occurrences.Add(identifier);
            }

            if (occurrences.Count != 1 && !IsRepeatable(argument))
                return null;

            foreach (var occurrence in occurrences)
            {
                ExpressionSyntax replacement = argument.WithoutTrivia();
                if (!IsAtomic(replacement) && NeedsParentheses(occurrence.Parent))
                    replacement = SyntaxFactory.ParenthesizedExpression(replacement);

                substitutions.Add(occurrence, replacement);
            }
        }

        var inlined = expression.ReplaceNodes(substitutions.Keys, (original, _) => substitutions[original].WithTriviaFrom(original));
        if (!IsAtomic(inlined) && NeedsParentheses(invocation.Parent))
            inlined = SyntaxFactory.ParenthesizedExpression(inlined);

        inlined = inlined.WithTriviaFrom(invocation).WithAdditionalAnnotations(Formatter.Annotation);
        return new InlinePlan(invocation, [inlined]);
    }

    private static InlinePlan? PlanStatements(
        MethodDeclarationSyntax method,
        IMethodSymbol symbol,
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        if (method.Body is not { } body)
            return null;

        var task = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var returnsTask = SymbolEqualityComparer.Default.Equals(symbol.ReturnType, task);
        if (!symbol.ReturnsVoid && !returnsTask)
            return null;

        var awaited = invocation.Parent is AwaitExpressionSyntax;
        var statement = (awaited ? invocation.Parent!.Parent : invocation.Parent) as ExpressionStatementSyntax;
        if (statement is null || statement.Parent is not BlockSyntax || (returnsTask && !awaited))
            return null;

        var nodes = body.DescendantNodes(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)).ToList();
        if (nodes.Any(node => node is ReturnStatementSyntax or YieldStatementSyntax or LabeledStatementSyntax or GotoStatementSyntax))
            return null;

        if (nodes.Any(node => IsAwait(node)) && !IsInAsyncFunction(statement))
            return null;

        if (HasNameConflict(body, symbol, statement, model, cancellationToken))
            return null;

        var callerNames = IdentifierNames(statement);
        var newline = statement.GetTrailingTrivia().LastOrDefault(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        if (!newline.IsKind(SyntaxKind.EndOfLineTrivia))
            newline = SyntaxFactory.CarriageReturnLineFeed;

        var replacements = new List<SyntaxNode>();
        foreach (var parameter in symbol.Parameters)
        {
            var argument = ArgumentFor(parameter, method, invocation);
            if (argument is null)
                return null;

            if (argument is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == parameter.Name)
            {
                var bound = model.GetSymbolInfo(identifier, cancellationToken).Symbol;
                if (bound is not (ILocalSymbol or IParameterSymbol) || IsWrittenIn(body, parameter, model, cancellationToken))
                    return null;

                continue;
            }

            if (callerNames.Contains(parameter.Name))
                return null;

            var typeInfo = model.GetTypeInfo(argument, cancellationToken);
            var inferable = typeInfo.Type is not null
                && SymbolEqualityComparer.IncludeNullability.Equals(typeInfo.Type, parameter.Type)
                && argument is not (LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression }
                    or ImplicitObjectCreationExpressionSyntax
                    or CollectionExpressionSyntax
                    or ImplicitStackAllocArrayCreationExpressionSyntax);
            var parameterSyntax = method.ParameterList.Parameters[parameter.Ordinal];
            var type = inferable ? SyntaxFactory.IdentifierName("var") : parameterSyntax.Type!.WithoutTrivia();
            var initializer = SyntaxFactory.EqualsValueClause(argument.WithoutTrivia());
            var declarator = SyntaxFactory.VariableDeclarator(parameter.Name).WithInitializer(initializer);
            var declarators = SyntaxFactory.SingletonSeparatedList(declarator);
            var declaration = SyntaxFactory.VariableDeclaration(type, declarators);
            var local = SyntaxFactory.LocalDeclarationStatement(declaration).WithTrailingTrivia(newline);
            replacements.Add(local);
        }

        foreach (var bodyStatement in body.Statements)
            replacements.Add(bodyStatement);

        var first = replacements[0];
        var ownLeading = first.GetLeadingTrivia().Where(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia));
        var leading = statement.GetLeadingTrivia().AddRange(ownLeading);
        replacements[0] = first.WithLeadingTrivia(leading);

        var last = replacements[replacements.Count - 1];
        replacements[replacements.Count - 1] = last.WithTrailingTrivia(statement.GetTrailingTrivia());

        var annotated = replacements.Select(node => node.WithAdditionalAnnotations(Formatter.Annotation));
        return new InlinePlan(statement, [.. annotated]);
    }

    // Every name the body declares must be new to the whole caller, and every name it reads must
    // still bind to the same symbol at the call site (a caller local of the same name would
    // capture it).
    private static bool HasNameConflict(
        SyntaxNode body,
        IMethodSymbol symbol,
        SyntaxNode callSite,
        SemanticModel model,
        CancellationToken cancellationToken)
    {
        var caller = callSite.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (caller is null)
            return true;

        var callerNames = IdentifierNames(callSite);
        var callerLocals = new HashSet<string>();
        foreach (var node in caller.DescendantNodes())
        {
            var declared = model.GetDeclaredSymbol(node, cancellationToken);
            if (IsLocalLike(declared))
                callerLocals.Add(declared!.Name);
        }

        foreach (var node in body.DescendantNodesAndSelf())
        {
            var declared = model.GetDeclaredSymbol(node, cancellationToken);
            if (IsLocalLike(declared))
            {
                if (callerNames.Contains(declared!.Name))
                    return true;

                continue;
            }

            if (node is not SimpleNameSyntax name || !IsSimpleName(name) || name is IdentifierNameSyntax { IsVar: true })
                continue;

            var bound = model.GetSymbolInfo(name, cancellationToken).Symbol;
            if (bound is null || IsLocalLike(bound))
                continue;

            if (bound is IParameterSymbol parameter && SymbolEqualityComparer.Default.Equals(parameter.ContainingSymbol, symbol))
                continue;

            var text = name.Identifier.ValueText;
            if (callerLocals.Contains(text))
                return true;

            var found = model.LookupSymbols(callSite.SpanStart, name: text);
            if (found.Any(candidate => !IsCompatible(candidate, bound)))
                return true;
        }

        return false;
    }

    private static bool IsLocalLike(ISymbol? symbol) =>
        symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or IMethodSymbol { MethodKind: MethodKind.LocalFunction };

    // Lookup by name returns every arity, so Task<T> sits beside Task without hiding it.
    private static bool IsCompatible(ISymbol found, ISymbol bound)
    {
        if (SymbolEqualityComparer.Default.Equals(found.OriginalDefinition, bound.OriginalDefinition))
            return true;

        if (found is INamedTypeSymbol foundType && bound is INamedTypeSymbol boundType)
            return foundType.Arity != boundType.Arity;

        return found is IMethodSymbol && bound is IMethodSymbol
            && SymbolEqualityComparer.Default.Equals(found.ContainingType, bound.ContainingType);
    }

    private static bool IsSimpleName(SimpleNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Expression == name,
        QualifiedNameSyntax qualified => qualified.Left == name,
        MemberBindingExpressionSyntax or AliasQualifiedNameSyntax or NameColonSyntax or NameEqualsSyntax or ExpressionColonSyntax => false,
        _ => true,
    };

    // Named-argument labels are the one identifier that names a parameter without conflicting
    // with a local of that name.
    private static HashSet<string> IdentifierNames(SyntaxNode callSite)
    {
        var caller = callSite.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? callSite;
        var names = new HashSet<string>();
        foreach (var token in caller.DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.IdentifierToken) || token.Parent is IdentifierNameSyntax { Parent: NameColonSyntax })
                continue;

            names.Add(token.ValueText);
        }

        return names;
    }

    private static ExpressionSyntax? ArgumentFor(
        IParameterSymbol parameter,
        MethodDeclarationSyntax method,
        InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (argument.NameColon is null ? index == parameter.Ordinal : argument.NameColon.Name.Identifier.ValueText == parameter.Name)
                return argument.Expression;
        }

        return method.ParameterList.Parameters[parameter.Ordinal].Default?.Value;
    }

    private static bool HasIdentityConversion(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
    {
        var typeInfo = model.GetTypeInfo(expression, cancellationToken);
        return typeInfo.Type is not null && SymbolEqualityComparer.Default.Equals(typeInfo.Type, typeInfo.ConvertedType);
    }

    private static bool IsWrittenIn(SyntaxNode body, IParameterSymbol parameter, SemanticModel model, CancellationToken cancellationToken)
    {
        foreach (var identifier in body.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (!IsWriteTarget(identifier))
                continue;

            var bound = model.GetSymbolInfo(identifier, cancellationToken).Symbol;
            if (SymbolEqualityComparer.Default.Equals(bound, parameter))
                return true;
        }

        return false;
    }

    private static bool IsWriteTarget(ExpressionSyntax expression) => expression.Parent switch
    {
        AssignmentExpressionSyntax assignment => assignment.Left == expression,
        PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax => expression.Parent.IsKind(SyntaxKind.PreIncrementExpression)
            || expression.Parent.IsKind(SyntaxKind.PreDecrementExpression)
            || expression.Parent.IsKind(SyntaxKind.PostIncrementExpression)
            || expression.Parent.IsKind(SyntaxKind.PostDecrementExpression),
        ArgumentSyntax argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None),
        RefExpressionSyntax => true,
        _ => false,
    };

    private static bool IsInsideNameOf(SyntaxNode node) =>
        node.Ancestors().OfType<InvocationExpressionSyntax>().Any(invocation =>
            invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });

    private static bool IsAwait(SyntaxNode node) => node switch
    {
        AwaitExpressionSyntax => true,
        CommonForEachStatementSyntax forEach => !forEach.AwaitKeyword.IsKind(SyntaxKind.None),
        UsingStatementSyntax usingStatement => !usingStatement.AwaitKeyword.IsKind(SyntaxKind.None),
        LocalDeclarationStatementSyntax declaration => !declaration.AwaitKeyword.IsKind(SyntaxKind.None),
        _ => false,
    };

    private static bool IsInAsyncFunction(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case AnonymousFunctionExpressionSyntax lambda:
                    return !lambda.AsyncKeyword.IsKind(SyntaxKind.None);
                case LocalFunctionStatementSyntax localFunction:
                    return localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case MethodDeclarationSyntax method:
                    return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
                case MemberDeclarationSyntax:
                    return false;
            }
        }

        return false;
    }

    private static bool IsStatementExpression(ExpressionSyntax expression) =>
        expression is InvocationExpressionSyntax
            or AssignmentExpressionSyntax
            or BaseObjectCreationExpressionSyntax
            or AwaitExpressionSyntax
            or ConditionalAccessExpressionSyntax
        || expression.IsKind(SyntaxKind.PreIncrementExpression)
        || expression.IsKind(SyntaxKind.PreDecrementExpression)
        || expression.IsKind(SyntaxKind.PostIncrementExpression)
        || expression.IsKind(SyntaxKind.PostDecrementExpression);

    private static bool NeedsParentheses(SyntaxNode? parent) =>
        parent is BinaryExpressionSyntax
            or PrefixUnaryExpressionSyntax
            or PostfixUnaryExpressionSyntax
            or ConditionalExpressionSyntax
            or CastExpressionSyntax
            or MemberAccessExpressionSyntax
            or ConditionalAccessExpressionSyntax
            or ElementAccessExpressionSyntax
            or InvocationExpressionSyntax
            or AwaitExpressionSyntax
            or IsPatternExpressionSyntax
            or SwitchExpressionSyntax
            or RangeExpressionSyntax
            or WithExpressionSyntax
            or InterpolationSyntax;

    private static bool IsAtomic(ExpressionSyntax expression) =>
        expression is SimpleNameSyntax
            or LiteralExpressionSyntax
            or MemberAccessExpressionSyntax
            or InvocationExpressionSyntax
            or ElementAccessExpressionSyntax
            or ThisExpressionSyntax
            or BaseExpressionSyntax
            or ParenthesizedExpressionSyntax
            or BaseObjectCreationExpressionSyntax
            or DefaultExpressionSyntax
            or TypeOfExpressionSyntax
            or InterpolatedStringExpressionSyntax
            or TupleExpressionSyntax
            or CollectionExpressionSyntax
            or ArrayCreationExpressionSyntax
            or ImplicitArrayCreationExpressionSyntax
            or AnonymousObjectCreationExpressionSyntax
            or PredefinedTypeSyntax;

    private static bool IsRepeatable(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax or LiteralExpressionSyntax or ThisExpressionSyntax or PredefinedTypeSyntax => true,
        DefaultExpressionSyntax or TypeOfExpressionSyntax => true,
        MemberAccessExpressionSyntax access => IsRepeatable(access.Expression),
        _ => false,
    };

    private static SyntaxNode WithoutLeadingBlankLines(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia();
        var index = 0;
        while (index < trivia.Count)
        {
            if (trivia[index].IsKind(SyntaxKind.EndOfLineTrivia))
                index++;
            else if (trivia[index].IsKind(SyntaxKind.WhitespaceTrivia) && index + 1 < trivia.Count && trivia[index + 1].IsKind(SyntaxKind.EndOfLineTrivia))
                index += 2;
            else
                break;
        }

        return node.WithLeadingTrivia(trivia.Skip(index));
    }

    private sealed class InlinePlan(SyntaxNode target, ImmutableArray<SyntaxNode> replacements)
    {
        public SyntaxNode Target { get; } = target;

        public ImmutableArray<SyntaxNode> Replacements { get; } = replacements;
    }
}
