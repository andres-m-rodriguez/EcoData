using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EcoData.Analyzers;

/// <summary>
/// ECO009: a warning suppression must state why the warning does not apply. A pragma warning
/// disable directive carries the reason in a trailing single-line comment, and a SuppressMessage
/// or UnconditionalSuppressMessage attribute carries it in a non-blank Justification. Restore
/// directives are never flagged, and attributes are matched by symbol so a same-named type from
/// another namespace is ignored.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SuppressionJustificationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO009";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Warning suppression needs a justification",
        "{0}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: a warning is fixed, not silenced. The rare suppression states why the warning does not apply, so the reason is reviewed with the code.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(treeContext => AnalyzePragmas(treeContext));
        context.RegisterCompilationStartAction(startContext => RegisterAttributeAnalysis(startContext));
    }

    private static void AnalyzePragmas(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var trivias = root.DescendantTrivia(descendIntoTrivia: false);
        foreach (var trivia in trivias)
        {
            if (!trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia))
                continue;

            if (trivia.GetStructure() is not PragmaWarningDirectiveTriviaSyntax directive)
                continue;

            if (!directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
                continue;

            if (HasJustificationComment(directive))
                continue;

            var ids = directive.ErrorCodes.Count == 0 ? "all warnings" : directive.ErrorCodes.ToString();
            var location = directive.GetLocation();
            var diagnostic = Diagnostic.Create(
                Rule,
                location,
                $"State why '{ids}' is disabled in a trailing comment on the pragma, or fix the warning");
            context.ReportDiagnostic(diagnostic);
        }
    }

    // The comment on a directive line is lexed as trivia of the directive's own tokens, so scanning
    // those tokens is enough to find it without looking at the surrounding line.
    private static bool HasJustificationComment(PragmaWarningDirectiveTriviaSyntax directive)
    {
        var tokens = directive.DescendantTokens();
        foreach (var token in tokens)
        {
            var trivias = token.LeadingTrivia.Concat(token.TrailingTrivia);
            foreach (var trivia in trivias)
            {
                if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                    continue;

                var text = trivia.ToString().Substring(2);
                if (!string.IsNullOrWhiteSpace(text))
                    return true;
            }
        }

        return false;
    }

    private static void RegisterAttributeAnalysis(CompilationStartAnalysisContext context)
    {
        var suppressMessage = context.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute");
        var unconditionalSuppressMessage = context.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute");
        if (suppressMessage is null && unconditionalSuppressMessage is null)
            return;

        context.RegisterSyntaxNodeAction(
            nodeContext => AnalyzeAttribute(nodeContext, suppressMessage, unconditionalSuppressMessage),
            SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? suppressMessage,
        INamedTypeSymbol? unconditionalSuppressMessage)
    {
        var attribute = (AttributeSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol constructor)
            return;

        var attributeType = constructor.ContainingType;
        var isSuppression = SymbolEqualityComparer.Default.Equals(attributeType, suppressMessage)
            || SymbolEqualityComparer.Default.Equals(attributeType, unconditionalSuppressMessage);
        if (!isSuppression)
            return;

        var arguments = attribute.ArgumentList?.Arguments ?? default;
        var justification = arguments.FirstOrDefault(argument => argument.NameEquals?.Name.Identifier.ValueText == "Justification");
        if (justification is not null)
        {
            var constant = context.SemanticModel.GetConstantValue(justification.Expression, context.CancellationToken);
            if (constant.Value is string text && !string.IsNullOrWhiteSpace(text))
                return;
        }

        var location = attribute.GetLocation();
        var diagnostic = Diagnostic.Create(
            Rule,
            location,
            "Set Justification on this SuppressMessage, or fix the warning");
        context.ReportDiagnostic(diagnostic);
    }
}
