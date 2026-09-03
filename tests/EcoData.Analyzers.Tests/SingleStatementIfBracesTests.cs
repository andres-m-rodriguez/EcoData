using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.SingleStatementIfBracesAnalyzer,
    EcoData.Analyzers.SingleStatementIfBracesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class SingleStatementIfBracesTests
{
    [Fact]
    public async Task SingleStatementIf_WithBraces_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                void M(bool condition)
                {
                    if (condition)
                    {|ECO001:{|}
                        Helper();
                    }
                }

                void Helper()
                {
                    System.Console.WriteLine("x");
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void M(bool condition)
                {
                    if (condition)
                        Helper();
                }

                void Helper()
                {
                    System.Console.WriteLine("x");
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ElseBlock_WithSingleStatement_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                int M(bool condition)
                {
                    if (condition)
                        return 1;
                    else
                    {|ECO001:{|}
                        return 2;
                    }
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(bool condition)
                {
                    if (condition)
                        return 1;
                    else
                        return 2;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task MultipleStatements_NotFlagged()
    {
        const string source = """
            class C
            {
                int M(bool condition)
                {
                    if (condition)
                    {
                        var x = 1;
                        return x;
                    }

                    return 0;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AlreadyBraceless_NotFlagged()
    {
        const string source = """
            class C
            {
                int M(bool condition)
                {
                    if (condition)
                        return 1;

                    return 0;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SingleLocalDeclaration_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(bool condition)
                {
                    if (condition)
                    {
                        var x = 1;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NestedIf_WithDanglingElseHazard_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(bool a, bool b)
                {
                    if (a)
                    {
                        if (b)
                            Helper();
                    }
                    else
                        Helper();
                }

                void Helper()
                {
                    System.Console.WriteLine("x");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SingleThrowStatement_IsFlaggedAndFixed()
    {
        const string source = """
            using System;

            class C
            {
                void M(bool condition)
                {
                    if (condition)
                    {|ECO001:{|}
                        throw new InvalidOperationException();
                    }
                }
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                void M(bool condition)
                {
                    if (condition)
                        throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task WhileLoopBody_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(bool condition)
                {
                    while (condition)
                    {
                        Helper();
                    }
                }

                void Helper()
                {
                    System.Console.WriteLine("x");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task BracesCarryingPreprocessorDirectives_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(bool condition)
                {
                    if (condition)
                    {
                        #region keep
                        Helper();
                        #endregion
                    }
                }

                void Helper()
                {
                    System.Console.WriteLine("x");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task BracesCarryingComments_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(bool condition)
                {
                    if (condition)
                    { // deliberate
                        Helper();
                    }
                }

                void Helper()
                {
                    System.Console.WriteLine("x");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
