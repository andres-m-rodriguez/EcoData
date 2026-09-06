using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.SuppressionJustificationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class SuppressionJustificationTests
{
    [Fact]
    public async Task PragmaDisable_WithoutComment_IsFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
            {|ECO009:#pragma warning disable CS0219|}
                    var unused = 1;
            #pragma warning restore CS0219
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task PragmaDisable_WithEmptyComment_IsFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
            {|ECO009:#pragma warning disable CS0219 //|}
                    var unused = 1;
            #pragma warning restore CS0219
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task PragmaDisable_WithWhitespaceOnlyComment_IsFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
            {|ECO009:#pragma warning disable CS0219 //   |}
                    var unused = 1;
            #pragma warning restore CS0219
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task PragmaDisable_WithJustificationComment_NotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
            #pragma warning disable CS0219 // The local documents the expected value
                    var unused = 1;
            #pragma warning restore CS0219
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task PragmaDisable_MessageListsIdsAsWritten()
    {
        const string source = """
            class C
            {
                void M()
                {
            #pragma warning disable CS0219, CS0168
                    var unused = 1;
            #pragma warning restore CS0219, CS0168
                }
            }
            """;

        var expected = VerifyCS.Diagnostic()
            .WithSpan(5, 1, 5, 39)
            .WithMessage("State why 'CS0219, CS0168' is disabled in a trailing comment on the pragma, or fix the warning");
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task PragmaDisable_WithoutIds_MessageSaysAllWarnings()
    {
        const string source = """
            class C
            {
                void M()
                {
            #pragma warning disable
                    var unused = 1;
            #pragma warning restore
                }
            }
            """;

        var expected = VerifyCS.Diagnostic()
            .WithSpan(5, 1, 5, 24)
            .WithMessage("State why 'all warnings' is disabled in a trailing comment on the pragma, or fix the warning");
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task PragmaRestore_NotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
            #pragma warning restore CS0219
                    var used = 1;
                    System.Console.WriteLine(used);
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SuppressMessage_WithoutJustification_IsFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [{|ECO009:SuppressMessage("Style", "IDE0001")|}]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SuppressMessage_WithJustification_NotFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [SuppressMessage("Style", "IDE0001", Justification = "Reflection reads this member")]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SuppressMessage_WithConstantJustification_NotFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                const string Reason = "Reflection reads this member";

                [SuppressMessage("Style", "IDE0001", Justification = Reason)]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SuppressMessage_WithEmptyJustification_IsFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [{|ECO009:SuppressMessage("Style", "IDE0001", Justification = "")|}]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SuppressMessage_WithWhitespaceJustification_IsFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [{|ECO009:SuppressMessage("Style", "IDE0001", Justification = "   ")|}]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SuppressMessage_WithNullJustification_IsFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [{|ECO009:SuppressMessage("Style", "IDE0001", Justification = null)|}]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SuppressMessage_MessageNamesTheAttribute()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [SuppressMessage("Style", "IDE0001")]
                void M()
                {
                }
            }
            """;

        var expected = VerifyCS.Diagnostic()
            .WithSpan(5, 6, 5, 41)
            .WithMessage("Set Justification on this SuppressMessage, or fix the warning");
        await VerifyAsync(source, expected);
    }

    [Fact]
    public async Task AssemblyLevelSuppressMessage_WithoutJustification_IsFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            [assembly: {|ECO009:SuppressMessage("Style", "IDE0001", Scope = "namespaceanddescendants", Target = "~N:App")|}]

            class C
            {
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task UnconditionalSuppressMessage_WithoutJustification_IsFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [{|ECO009:UnconditionalSuppressMessage("Trimming", "IL2026")|}]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source, ReferenceAssemblies.Net.Net80);
    }

    [Fact]
    public async Task UnconditionalSuppressMessage_WithJustification_NotFlagged()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The member is preserved by the descriptor")]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source, ReferenceAssemblies.Net.Net80);
    }

    [Fact]
    public async Task SameNamedAttributeFromAnotherNamespace_NotFlagged()
    {
        const string source = """
            using System;

            namespace App
            {
                [AttributeUsage(AttributeTargets.All)]
                sealed class SuppressMessageAttribute : Attribute
                {
                    public SuppressMessageAttribute(string category, string checkId)
                    {
                    }
                }

                class C
                {
                    [SuppressMessage("Style", "IDE0001")]
                    void M()
                    {
                    }
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task OtherAttribute_NotFlagged()
    {
        const string source = """
            using System;

            class C
            {
                [Obsolete("Use N instead")]
                void M()
                {
                }
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task GeneratedFile_NotFlagged()
    {
        const string source = """
            // <auto-generated/>
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [SuppressMessage("Style", "IDE0001")]
                void M()
                {
            #pragma warning disable CS0219
                    var unused = 1;
            #pragma warning restore CS0219
                }
            }
            """;

        await VerifyAsync(source);
    }

    // The verifier's suppression check prepends a bare "#pragma warning disable ECO009" to the
    // source, which this rule flags by design, so that check is skipped for every test.
    private static async Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        await VerifyAsync(source, ReferenceAssemblies.Default, expected);
    }

    private static async Task VerifyAsync(string source, ReferenceAssemblies referenceAssemblies, params DiagnosticResult[] expected)
    {
        var test = new CSharpCodeFixTest<SuppressionJustificationAnalyzer, EmptyCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = referenceAssemblies,
            TestBehaviors = TestBehaviors.SkipSuppressionCheck,
        };
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
