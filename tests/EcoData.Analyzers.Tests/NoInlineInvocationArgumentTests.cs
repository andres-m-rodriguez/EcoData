using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.NoInlineInvocationArgumentAnalyzer,
    EcoData.Analyzers.NoInlineInvocationArgumentCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class NoInlineInvocationArgumentTests
{
    [Fact]
    public async Task InvocationArgument_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                void M()
                {
                    Use({|ECO002:GetValue()|});
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        const string fixedSource = """
            class C
            {
                void M()
                {
                    var value = GetValue();
                    Use(value);
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task AwaitedInvocationArgument_IsFlaggedAndFixed()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    Use(await {|ECO002:GetValueAsync()|});
                }

                void Use(int number)
                {
                }

                async Task<int> GetValueAsync()
                {
                    await Task.Yield();
                    return 42;
                }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    var value = await GetValueAsync();
                    Use(value);
                }

                void Use(int number)
                {
                }

                async Task<int> GetValueAsync()
                {
                    await Task.Yield();
                    return 42;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NestedCalls_BothFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    Use({|ECO002:Combine({|ECO002:GetValue()|})|});
                }

                void Use(int number)
                {
                }

                int Combine(int number) => number;

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NameofAndLocalArguments_NotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var value = GetValue();
                    Use(value);
                    Log(nameof(M));
                }

                void Use(int number)
                {
                }

                void Log(string message)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task LambdaExpressionBody_NotFlagged()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Run(() => Use(GetValue()));
                }

                void Run(Action action)
                {
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StatementInLambdaBlockBody_IsFlaggedAndFixed()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Run(() =>
                    {
                        Use({|ECO002:GetValue()|});
                    });
                }

                void Run(Action action)
                {
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                void M()
                {
                    Run(() =>
                    {
                        var value = GetValue();
                        Use(value);
                    });
                }

                void Run(Action action)
                {
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NameCollision_AppendsNumericSuffix()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var value = 1;
                    Use(value);
                    Use({|ECO002:GetValue()|});
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        const string fixedSource = """
            class C
            {
                void M()
                {
                    var value = 1;
                    Use(value);
                    var value2 = GetValue();
                    Use(value2);
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ConstructorArgument_NotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var wrapper = new Wrapper(GetValue());
                    Use(wrapper);
                }

                void Use(Wrapper wrapper)
                {
                }

                int GetValue() => 42;
            }

            class Wrapper
            {
                public Wrapper(int number)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConditionalAccessArgument_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(string text)
                {
                    Use(text?.Trim());
                }

                void Use(string value)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CatchFilter_NotFlagged()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    try
                    {
                        Helper();
                    }
                    catch (Exception exception) when (Check(GetCode(exception)))
                    {
                    }
                }

                void Helper()
                {
                }

                bool Check(int code) => code == 0;

                int GetCode(Exception exception) => exception.HResult;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task WhileCondition_NotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var count = 0;
                    while (Check(Next(count)))
                    {
                        count = count + 1;
                    }
                }

                bool Check(int number) => number < 3;

                int Next(int number) => number + 1;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConditionalAccessReceiver_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(C target)
                {
                    target?.Use(GetValue());
                }

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RefArgument_NotFlagged()
    {
        const string source = """
            class C
            {
                int field;

                void M()
                {
                    Mutate(ref GetRef());
                }

                void Mutate(ref int number)
                {
                }

                ref int GetRef() => ref field;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ArgumentAfterSideEffectingArgument_NotFlagged()
    {
        const string source = """
            class C
            {
                int counter;

                void M()
                {
                    Use(++counter, GetValue());
                }

                void Use(int first, int second)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SwitchCaseWhenClause_NotFlagged()
    {
        const string source = """
            class C
            {
                void M(object item)
                {
                    switch (item)
                    {
                        case int number when Check(Transform(number)):
                            break;
                    }
                }

                bool Check(int number) => number == 0;

                int Transform(int number) => number + 1;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SideEffectingReceiver_NotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    GetTarget().Use(GetValue());
                }

                C GetTarget() => this;

                void Use(int number)
                {
                }

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NestedCallAfterSideEffectingOuterArgument_NotFlagged()
    {
        const string source = """
            class C
            {
                int counter;

                void M()
                {
                    Use(++counter, Combine(GetValue()));
                }

                void Use(int first, int second)
                {
                }

                int Combine(int number) => number;

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ObjectInitializerContext_NotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var wrapper = new Wrapper { Number = Combine(GetValue()) };
                    Use(wrapper);
                }

                void Use(Wrapper wrapper)
                {
                }

                int Combine(int number) => number;

                int GetValue() => 42;
            }

            class Wrapper
            {
                public int Number { get; set; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ExpressionBodiedMethod_NotFlagged()
    {
        const string source = """
            class C
            {
                int M() => Combine(GetValue());

                int Combine(int number) => number;

                int GetValue() => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
