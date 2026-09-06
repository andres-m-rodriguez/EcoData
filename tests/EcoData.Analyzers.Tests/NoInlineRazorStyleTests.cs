using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    EcoData.Analyzers.NoInlineRazorStyleAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class NoInlineRazorStyleTests
{
    private const string AttributeMessage = "Replace the inline style with a CSS class; sizing for JS interop hosts belongs in a class too";
    private const string ElementMessage = "Move the style element into the component's .razor.css or the app stylesheet";

    [Fact]
    public async Task StyleAttribute_IsFlagged()
    {
        const string razor = """
            <div style="color: red"></div>
            """;

        var test = CreateTest("Component.razor", razor);
        var expected = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 1, 6, 1, 24);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task SingleQuotedStyleAttribute_IsFlagged()
    {
        const string razor = """
            <span style='margin: 0'>x</span>
            """;

        var test = CreateTest("Component.razor", razor);
        var expected = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 1, 7, 1, 24);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task BoundStyleAttribute_IsFlagged()
    {
        const string razor = """
            <div style="@Style"></div>
            <div style="width:@Width"></div>
            <div style=@Style></div>
            """;

        var test = CreateTest("Component.razor", razor);
        var first = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 1, 6, 1, 20);
        var second = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 2, 6, 2, 26);
        var third = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 3, 6, 3, 18);
        test.ExpectedDiagnostics.Add(first);
        test.ExpectedDiagnostics.Add(second);
        test.ExpectedDiagnostics.Add(third);
        await test.RunAsync();
    }

    [Fact]
    public async Task ComponentStyleParameter_IsFlagged()
    {
        const string razor = """
            <MudPaper Style="height: 100px" />
            """;

        var test = CreateTest("Component.razor", razor);
        var expected = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 1, 11, 1, 32);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task TwoStyleAttributesOnOneLine_AreBothFlagged()
    {
        const string razor = """
            <p style="a: b"><b style="c: d">x</b></p>
            """;

        var test = CreateTest("Component.razor", razor);
        var first = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 1, 4, 1, 16);
        var second = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 1, 20, 1, 32);
        test.ExpectedDiagnostics.Add(first);
        test.ExpectedDiagnostics.Add(second);
        await test.RunAsync();
    }

    [Fact]
    public async Task MultiLineFile_ReportsAccuratePosition()
    {
        const string razor = """
            @page "/demo"

            <h1>Title</h1>
            <section>
                <div class="card" style="padding: 4px"></div>
            </section>

            @code {
                private int count;
            }
            """;

        var test = CreateTest("Component.razor", razor);
        var expected = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 5, 23, 5, 43);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task StyleElement_IsFlaggedOnceAtOpeningTag()
    {
        const string razor = """
            <style>
                .host { display: block; }
                div style="x" { }
            </style>
            <div class="host"></div>
            """;

        var test = CreateTest("Component.razor", razor);
        var expected = VerifyCS.Diagnostic().WithMessage(ElementMessage).WithSpan("Component.razor", 1, 1, 1, 8);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task StyleElementWithAttributes_IsFlaggedAtOpeningTag()
    {
        const string razor = """
            <div class="host"></div>

            <STYLE type="text/css">
                .host { display: block; }
            </STYLE>
            """;

        var test = CreateTest("Component.razor", razor);
        var expected = VerifyCS.Diagnostic().WithMessage(ElementMessage).WithSpan("Component.razor", 3, 1, 3, 24);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    [Fact]
    public async Task StyleElementAndAttribute_AreBothFlagged()
    {
        const string razor = """
            <style>.host { color: red; }</style>
            <div style="color: red"></div>
            """;

        var test = CreateTest("Component.razor", razor);
        var element = VerifyCS.Diagnostic().WithMessage(ElementMessage).WithSpan("Component.razor", 1, 1, 1, 8);
        var attribute = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.razor", 2, 6, 2, 24);
        test.ExpectedDiagnostics.Add(element);
        test.ExpectedDiagnostics.Add(attribute);
        await test.RunAsync();
    }

    [Fact]
    public async Task RazorComment_NotFlagged()
    {
        const string razor = """
            @* <div style="color: red"></div> *@
            @*
                <div style="color: red"></div>
            *@
            <div class="x"></div>
            """;

        var test = CreateTest("Component.razor", razor);
        await test.RunAsync();
    }

    [Fact]
    public async Task CodeBlockIdentifier_NotFlagged()
    {
        const string razor = """
            <div class="x"></div>

            @code {
                private string style = "{";
                private string Build() => $"height: {Height}; width: {Width};";
                // don't count this style = "x"
            }
            """;

        var test = CreateTest("Component.razor", razor);
        await test.RunAsync();
    }

    [Fact]
    public async Task DataStyleAttribute_NotFlagged()
    {
        const string razor = """
            <div data-style="compact" class="x" mystyle="y"></div>
            """;

        var test = CreateTest("Component.razor", razor);
        await test.RunAsync();
    }

    [Fact]
    public async Task CshtmlFile_NotFlagged()
    {
        const string cshtml = """
            <div style="color: red"></div>
            """;

        var test = CreateTest("Page.cshtml", cshtml);
        await test.RunAsync();
    }

    [Fact]
    public async Task CleanFile_NotFlagged()
    {
        const string razor = """
            @page "/demo"

            <div class="card">
                <span class="label">@Text</span>
            </div>

            @code {
                [Parameter]
                public string? Text { get; set; }
            }
            """;

        var test = CreateTest("Component.razor", razor);
        await test.RunAsync();
    }

    [Fact]
    public async Task UpperCaseExtension_IsScanned()
    {
        const string razor = """
            <div style="color: red"></div>
            """;

        var test = CreateTest("Component.RAZOR", razor);
        var expected = VerifyCS.Diagnostic().WithMessage(AttributeMessage).WithSpan("Component.RAZOR", 1, 6, 1, 24);
        test.ExpectedDiagnostics.Add(expected);
        await test.RunAsync();
    }

    private static CSharpAnalyzerTest<NoInlineRazorStyleAnalyzer, DefaultVerifier> CreateTest(string fileName, string content)
    {
        var test = new CSharpAnalyzerTest<NoInlineRazorStyleAnalyzer, DefaultVerifier>();
        test.TestState.Sources.Add("class C { }");
        test.TestState.AdditionalFiles.Add((fileName, content));
        return test;
    }
}
