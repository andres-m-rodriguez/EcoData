using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.NoMethodGroupArgumentAnalyzer,
    EcoData.Analyzers.NoMethodGroupArgumentCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class NoMethodGroupArgumentTests
{
    [Fact]
    public async Task InstanceMethodGroup_IsFlaggedAndFixed()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Run({|ECO003:Work|});
                }

                void Run(Func<int, int> callback)
                {
                }

                int Work(int number) => number;
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                void M()
                {
                    Run(number => Work(number));
                }

                void Run(Func<int, int> callback)
                {
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task StaticMemberAccessMethodGroup_IsFlaggedAndFixed()
    {
        const string source = """
            using System;

            static class Helper
            {
                public static int Twice(int number) => number * 2;
            }

            class C
            {
                void M()
                {
                    Run({|ECO003:Helper.Twice|});
                }

                void Run(Func<int, int> callback)
                {
                }
            }
            """;

        const string fixedSource = """
            using System;

            static class Helper
            {
                public static int Twice(int number) => number * 2;
            }

            class C
            {
                void M()
                {
                    Run(number => Helper.Twice(number));
                }

                void Run(Func<int, int> callback)
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task TwoParameterDelegate_IsFixedWithParenthesizedLambda()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Run({|ECO003:Add|});
                }

                void Run(Func<int, int, int> callback)
                {
                }

                int Add(int left, int right) => left + right;
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                void M()
                {
                    Run((left, right) => Add(left, right));
                }

                void Run(Func<int, int, int> callback)
                {
                }

                int Add(int left, int right) => left + right;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ParameterlessDelegate_IsFixedWithEmptyLambda()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Run({|ECO003:Work|});
                }

                void Run(Action callback)
                {
                }

                void Work()
                {
                }
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                void M()
                {
                    Run(() => Work());
                }

                void Run(Action callback)
                {
                }

                void Work()
                {
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ConstructorArgument_IsFlaggedAndFixed()
    {
        const string source = """
            using System;

            class Wrapper
            {
                public Wrapper(Func<int, int> callback)
                {
                }
            }

            class C
            {
                void M()
                {
                    var wrapper = new Wrapper({|ECO003:Work|});
                }

                int Work(int number) => number;
            }
            """;

        const string fixedSource = """
            using System;

            class Wrapper
            {
                public Wrapper(Func<int, int> callback)
                {
                }
            }

            class C
            {
                void M()
                {
                    var wrapper = new Wrapper(number => Work(number));
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ParameterNameAlreadyInScope_GetsSuffix()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    var number = 1;
                    Run({|ECO003:Work|});
                }

                void Run(Func<int, int> callback)
                {
                }

                int Work(int number) => number;
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                void M()
                {
                    var number = 1;
                    Run(number2 => Work(number2));
                }

                void Run(Func<int, int> callback)
                {
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task GenericMethodGroup_IsFlaggedAndFixed()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Run({|ECO003:Identity<int>|});
                }

                void Run(Func<int, int> callback)
                {
                }

                T Identity<T>(T value) => value;
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                void M()
                {
                    Run(value => Identity<int>(value));
                }

                void Run(Func<int, int> callback)
                {
                }

                T Identity<T>(T value) => value;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task Lambda_IsNotFlagged()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Run(number => Work(number));
                }

                void Run(Func<int, int> callback)
                {
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DelegateLocal_IsNotFlagged()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> callback = number => Work(number);
                    Run(callback);
                }

                void Run(Func<int, int> callback)
                {
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InvocationResult_IsNotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var value = Work(1);
                    Use(value);
                }

                void Use(int value)
                {
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NameOf_IsNotFlagged()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var name = nameof(Work);
                    Use(name);
                }

                void Use(string name)
                {
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Assignment_IsNotFlagged()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> callback = Work;
                    callback(1);
                }

                int Work(int number) => number;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
