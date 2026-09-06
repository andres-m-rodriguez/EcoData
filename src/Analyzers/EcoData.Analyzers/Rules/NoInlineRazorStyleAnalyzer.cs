using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace EcoData.Analyzers;

/// <summary>
/// ECO010: Razor markup must not carry a style attribute. Every .razor additional file is scanned
/// as text, so bound values such as style="@Style" and component Style parameters are flagged
/// along with literal styles. A style element is flagged once at its opening tag. Razor comments,
/// style element contents, and @code blocks are blanked before matching so their contents never
/// count, and .cshtml files are left alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoInlineRazorStyleAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ECO010";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Razor markup should not use inline styles",
        "{0}",
        "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "EcoData convention: Blazor markup never carries a style attribute or a style element. Every visual rule lives in a stylesheet, including sizes that a JavaScript library needs on its host element.");

    private const string AttributeMessage = "Replace the inline style with a CSS class; sizing for JS interop hosts belongs in a class too";
    private const string ElementMessage = "Move the style element into the component's .razor.css or the app stylesheet";

    private static readonly Regex StyleAttribute = new(
        @"(?<=[\s""'])style\s*=\s*(?:""[^""]*""|'[^']*'|@?[^\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RazorComment = new(@"@\*.*?\*@", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex StyleElement = new(
        @"(?<open><style\b[^>]*>).*?</style\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CodeBlockStart = new(@"@(?:code|functions)\s*\{", RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterAdditionalFileAction(fileContext => AnalyzeFile(fileContext));
    }

    private static void AnalyzeFile(AdditionalFileAnalysisContext context)
    {
        var path = context.AdditionalFile.Path;
        if (!path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            return;

        var text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null)
            return;

        var source = text.ToString();
        var markup = source.ToCharArray();

        var comments = RazorComment.Matches(source);
        foreach (Match comment in comments)
            Blank(markup, comment.Index, comment.Index + comment.Length);

        // The element is reported once at its opening tag, and its CSS is blanked so a selector
        // such as div[style] does not count a second time as an attribute.
        var styleElements = StyleElement.Matches(source);
        foreach (Match styleElement in styleElements)
        {
            var open = styleElement.Groups["open"];
            var span = new TextSpan(open.Index, open.Length);
            var lineSpan = text.Lines.GetLinePositionSpan(span);
            var location = Location.Create(path, span, lineSpan);
            var diagnostic = Diagnostic.Create(Rule, location, ElementMessage);
            context.ReportDiagnostic(diagnostic);
            Blank(markup, styleElement.Index, styleElement.Index + styleElement.Length);
        }

        // Only @code and @functions are skipped: @{ } blocks and control flow can carry markup.
        // The C# lexer finds the closing brace so braces inside strings and comments do not count.
        var codeBlocks = CodeBlockStart.Matches(source);
        foreach (Match codeBlock in codeBlocks)
        {
            var openBrace = codeBlock.Index + codeBlock.Length - 1;
            var end = source.Length;
            var depth = 0;
            var tokens = SyntaxFactory.ParseTokens(source, openBrace, openBrace);
            foreach (var token in tokens)
            {
                if (token.IsKind(SyntaxKind.OpenBraceToken))
                    depth++;

                if (!token.IsKind(SyntaxKind.CloseBraceToken))
                    continue;

                depth--;
                if (depth > 0)
                    continue;

                end = token.Span.End;
                break;
            }

            Blank(markup, codeBlock.Index, end);
        }

        var scannable = new string(markup);
        var matches = StyleAttribute.Matches(scannable);
        foreach (Match match in matches)
        {
            var span = new TextSpan(match.Index, match.Length);
            var lineSpan = text.Lines.GetLinePositionSpan(span);
            var location = Location.Create(path, span, lineSpan);
            var diagnostic = Diagnostic.Create(Rule, location, AttributeMessage);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void Blank(char[] markup, int start, int end)
    {
        for (var index = start; index < end; index++)
            markup[index] = ' ';
    }
}
