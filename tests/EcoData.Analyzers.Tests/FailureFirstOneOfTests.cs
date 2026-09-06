using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.FailureFirstOneOfAnalyzer,
    EcoData.Analyzers.FailureFirstOneOfCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class FailureFirstOneOfTests
{
    // Stand-in for OneOf<T, TError>: the analyzer is structural, so only the member names matter.
    private const string Result = """


        class Result<T, TError>
        {
            public bool IsT0 { get; }
            public bool IsT1 { get; }

            public bool TryPickT0(out T value, out TError remainder)
            {
                value = default;
                remainder = default;
                return IsT0;
            }

            public bool TryPickT1(out TError value, out T remainder)
            {
                value = default;
                remainder = default;
                return IsT1;
            }
        }
        """;

    [Fact]
    public async Task IsT0_WithElseReturn_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result, int fallback)
                {
                    if ({|ECO006:result.IsT0|})
                    {
                        _count = fallback;
                        _error = null;
                    }
                    else
                    {
                        _error = "failed";
                        return;
                    }

                    Log();
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result, int fallback)
                {
                    if (!result.IsT0)
                    {
                        _error = "failed";
                        return;
                    }

                    _count = fallback;
                    _error = null;

                    Log();
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task TryPickT0_WithElseThrow_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result)
                {
                    if ({|ECO006:result.TryPickT0(out var count, out var error)|})
                    {
                        _count = count;
                        _error = null;
                    }
                    else
                        throw new System.InvalidOperationException(error);
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result)
                {
                    if (!result.TryPickT0(out var count, out var error))
                        throw new System.InvalidOperationException(error);

                    _count = count;
                    _error = null;
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task SingleStatementBranches_OnOneLine_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                void M(Result<int, string> result)
                {
                    if ({|ECO006:result.IsT0|}) Use(); else return;
                }

                void Use()
                {
                    System.Console.WriteLine("x");
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                void M(Result<int, string> result)
                {
                    if (!result.IsT0) return;

                    Use();
                }

                void Use()
                {
                    System.Console.WriteLine("x");
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task SingleStatementBranches_OnSeparateLines_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                void M(Result<int, string> result)
                {
                    if ({|ECO006:result.TryPickT0(out var value, out _)|})
                        Use(value);
                    else
                        return;
                }

                void Use(int value)
                {
                    System.Console.WriteLine(value);
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                void M(Result<int, string> result)
                {
                    if (!result.TryPickT0(out var value, out _))
                        return;

                    Use(value);
                }

                void Use(int value)
                {
                    System.Console.WriteLine(value);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ElseContinue_InsideLoop_IsFlaggedAndFixed()
    {
        const string source = """
            using System.Collections.Generic;

            class C
            {
                readonly List<int> _values = new List<int>();

                void M(IEnumerable<Result<int, string>> results)
                {
                    foreach (var result in results)
                    {
                        if ({|ECO006:result.TryPickT0(out var value, out var error)|})
                        {
                            _values.Add(value);
                            Log(value.ToString());
                        }
                        else
                        {
                            Log(error);
                            continue;
                        }
                    }
                }

                void Log(string message)
                {
                    System.Console.WriteLine(message);
                }
            }
            """ + Result;

        const string fixedSource = """
            using System.Collections.Generic;

            class C
            {
                readonly List<int> _values = new List<int>();

                void M(IEnumerable<Result<int, string>> results)
                {
                    foreach (var result in results)
                    {
                        if (!result.TryPickT0(out var value, out var error))
                        {
                            Log(error);
                            continue;
                        }

                        _values.Add(value);
                        Log(value.ToString());
                    }
                }

                void Log(string message)
                {
                    System.Console.WriteLine(message);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ParenthesizedCondition_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                void M(Result<int, string> result)
                {
                    if ({|ECO006:(result.IsT0)|})
                        Use();
                    else
                        return;
                }

                void Use()
                {
                    System.Console.WriteLine("x");
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                void M(Result<int, string> result)
                {
                    if (!result.IsT0)
                        return;

                    Use();
                }

                void Use()
                {
                    System.Console.WriteLine("x");
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task Comments_MoveWithTheirStatements()
    {
        const string source = """
            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    if ({|ECO006:result.TryPickT0(out var count, out _)|})
                    {
                        // keep the last good value
                        _count = count;
                        Log(); // and tell someone
                    }
                    else
                    {
                        // nothing to show
                        return;
                    }
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    if (!result.TryPickT0(out var count, out _))
                    {
                        // nothing to show
                        return;
                    }

                    // keep the last good value
                    _count = count;
                    Log(); // and tell someone
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoElse_ClosingVoidMethod_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result)
                {
                    if ({|ECO006:result.TryPickT0(out var count, out var error)|})
                    {
                        _count = count;
                        _error = null;
                    }
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result)
                {
                    if (!result.TryPickT0(out var count, out var error))
                        return;

                    _count = count;
                    _error = null;
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoElse_ClosingAsyncTaskMethod_IsFlaggedAndFixed()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                int _count;

                async Task M(Result<int, string> result)
                {
                    await Task.Yield();
                    if ({|ECO006:result.TryPickT0(out var count, out _)|})
                    {
                        _count = count;
                        await Task.Yield();
                    }
                }
            }
            """ + Result;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                int _count;

                async Task M(Result<int, string> result)
                {
                    await Task.Yield();
                    if (!result.TryPickT0(out var count, out _))
                        return;

                    _count = count;
                    await Task.Yield();
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoElse_ClosingAsyncLambda_IsFlaggedAndFixed()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    Func<Task> load = async () =>
                    {
                        await Task.Yield();
                        if ({|ECO006:result.TryPickT0(out var count, out _)|})
                        {
                            _count = count;
                            await Task.Yield();
                        }
                    };
                }
            }
            """ + Result;

        const string fixedSource = """
            using System;
            using System.Threading.Tasks;

            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    Func<Task> load = async () =>
                    {
                        await Task.Yield();
                        if (!result.TryPickT0(out var count, out _))
                            return;

                        _count = count;
                        await Task.Yield();
                    };
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoElse_ClosingTryBlock_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    try
                    {
                        if ({|ECO006:result.TryPickT0(out var count, out _)|})
                        {
                            _count = count;
                            Log();
                        }
                    }
                    catch (System.Exception)
                    {
                        _count = 0;
                    }
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        const string fixedSource = """
            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    try
                    {
                        if (!result.TryPickT0(out var count, out _))
                            return;

                        _count = count;
                        Log();
                    }
                    catch (System.Exception)
                    {
                        _count = 0;
                    }
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NoElse_ClosingValueReturningMember_IsFlaggedWithoutFix()
    {
        const string source = """
            using System.Collections.Generic;

            class C
            {
                int _count;

                IEnumerable<int> M(Result<int, string> result)
                {
                    yield return _count;
                    if ({|ECO006:result.TryPickT0(out var count, out _)|})
                    {
                        _count = count;
                        Log();
                    }
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task NoElse_NestedUnderStatementsThatFollow_IsFlaggedWithoutFix()
    {
        const string source = """
            class C
            {
                int _count;

                void M(Result<int, string> result, bool enabled)
                {
                    if (enabled)
                    {
                        if ({|ECO006:result.TryPickT0(out var count, out _)|})
                        {
                            _count = count;
                            Log();
                        }
                    }

                    Log();
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task NoElse_ClosingLoopBody_IsFlaggedWithoutFix()
    {
        const string source = """
            using System.Collections.Generic;

            class C
            {
                readonly List<int> _values = new List<int>();

                void M(IEnumerable<Result<int, string>> results)
                {
                    foreach (var result in results)
                    {
                        if ({|ECO006:result.TryPickT0(out var value, out _)|})
                        {
                            _values.Add(value);
                            Log(value);
                        }
                    }
                }

                void Log(int value)
                {
                    System.Console.WriteLine(value);
                }
            }
            """ + Result;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task NoElse_NotLastStatement_NotFlagged()
    {
        const string source = """
            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    if (result.TryPickT0(out var count, out _))
                    {
                        _count = count;
                        Log();
                    }

                    Log();
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NoElse_SingleStatementBody_NotFlagged()
    {
        const string source = """
            class C
            {
                int _count;

                void M(Result<int, string> result)
                {
                    if (result.TryPickT0(out var count, out _)) _count = count;
                }
            }
            """ + Result;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NegatedCondition_NotFlagged()
    {
        const string source = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result)
                {
                    if (!result.TryPickT0(out var count, out var error))
                    {
                        _error = error;
                        return;
                    }

                    _count = count;
                    _error = null;
                }

                void N(Result<int, string> result)
                {
                    if (!result.IsT0)
                        return;

                    _count = 1;
                }
            }
            """ + Result;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task FailureChecks_NotFlagged()
    {
        const string source = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result)
                {
                    if (result.IsT1)
                    {
                        _error = "failed";
                        _count = 0;
                    }
                    else
                        return;
                }

                void N(Result<int, string> result)
                {
                    if (result.TryPickT1(out var error, out _))
                    {
                        _error = error;
                        _count = 0;
                    }
                }
            }
            """ + Result;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ThenBodyEndsInJump_NotFlagged()
    {
        const string source = """
            class C
            {
                string _error;

                int M(Result<int, string> result)
                {
                    if (result.TryPickT0(out var count, out var error))
                    {
                        _error = null;
                        return count;
                    }

                    _error = error;
                    return 0;
                }
            }
            """ + Result;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ElseIfChain_NotFlagged()
    {
        const string source = """
            class C
            {
                int _count;

                void M(Result<int, string> result, bool retry)
                {
                    if (result.TryPickT0(out var count, out _))
                    {
                        _count = count;
                        Log();
                    }
                    else if (retry)
                        return;
                    else
                        return;
                }

                void Log()
                {
                    System.Console.WriteLine(_count);
                }
            }
            """ + Result;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ElseWithoutJump_NotFlagged()
    {
        const string source = """
            class C
            {
                int _count;
                string _error;

                void M(Result<int, string> result)
                {
                    if (result.TryPickT0(out var count, out var error))
                    {
                        _count = count;
                        _error = null;
                    }
                    else
                    {
                        _error = error;
                    }
                }
            }
            """ + Result;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
