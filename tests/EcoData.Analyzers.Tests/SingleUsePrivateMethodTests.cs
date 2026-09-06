using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.SingleUsePrivateMethodAnalyzer,
    EcoData.Analyzers.SingleUsePrivateMethodCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class SingleUsePrivateMethodTests
{
    [Fact]
    public async Task ExpressionBodiedMethod_IsInlined()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var result = Twice(value);
                    return result;
                }

                private int {|ECO005:Twice|}(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int value)
                {
                    var result = value * 2;
                    return result;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task SingleReturnBlock_IsInlined()
    {
        const string source = """
            class C
            {
                void M(string who)
                {
                    System.Console.WriteLine(Greeting(who));
                }

                private string {|ECO005:Greeting|}(string name)
                {
                    return "Hello " + name;
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void M(string who)
                {
                    System.Console.WriteLine("Hello " + who);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CompoundArgument_IsParenthesizedWhereNeeded()
    {
        const string source = """
            class C
            {
                int M(int left, int right)
                {
                    var total = Twice(left + right);
                    return total;
                }

                private int {|ECO005:Twice|}(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int left, int right)
                {
                    var total = (left + right) * 2;
                    return total;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task InlinedExpressionInsideOperator_IsParenthesized()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var total = Twice(value) + 1;
                    return total;
                }

                private int {|ECO005:Twice|}(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int value)
                {
                    var total = (value * 2) + 1;
                    return total;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NamedArguments_AreSubstitutedByName()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var scaled = Scale(factor: 3, number: value);
                    return scaled;
                }

                private int {|ECO005:Scale|}(int number, int factor) => number * factor;
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int value)
                {
                    var scaled = value * 3;
                    return scaled;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task OmittedOptionalArgument_TakesDefaultValue()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var scaled = Scale(value);
                    return scaled;
                }

                private int {|ECO005:Scale|}(int number, int factor = 4) => number * factor;
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int value)
                {
                    var scaled = value * 4;
                    return scaled;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task AwaitedAsyncExpressionBody_IsInlinedUnderCallerAwait()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    await Pause();
                }

                private async Task {|ECO005:Pause|}() => await Task.Delay(1);
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    await Task.Delay(1);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ThisQualifiedCall_IsInlined()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var result = this.Twice(value);
                    return result;
                }

                private int {|ECO005:Twice|}(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int value)
                {
                    var result = value * 2;
                    return result;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task StatementBody_IsSplicedWithParameterLocal()
    {
        const string source = """
            class C
            {
                void M(int total)
                {
                    Log(total + 1);
                }

                private void {|ECO005:Log|}(int number)
                {
                    var text = number.ToString();
                    System.Console.WriteLine(text);
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void M(int total)
                {
                    var number = total + 1;
                    var text = number.ToString();
                    System.Console.WriteLine(text);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task SameNameArgument_IsDropped()
    {
        const string source = """
            class C
            {
                void M(int number)
                {
                    Log(number);
                }

                private void {|ECO005:Log|}(int number)
                {
                    System.Console.WriteLine(number);
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                void M(int number)
                {
                    System.Console.WriteLine(number);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task AwaitedTaskStatementBody_IsSpliced()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    await Save();
                }

                private async Task {|ECO005:Save|}()
                {
                    await Task.Delay(1);
                    System.Console.WriteLine("saved");
                }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                async Task M()
                {
                    await Task.Delay(1);
                    System.Console.WriteLine("saved");
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task FirstMember_IsRemovedWithoutLeavingBlankLine()
    {
        const string source = """
            class C
            {
                private int {|ECO005:Twice|}(int number) => number * 2;

                int M(int value)
                {
                    var result = Twice(value);
                    return result;
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int value)
                {
                    var result = value * 2;
                    return result;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task VoidExpressionBodyCalledAsStatement_IsInlined()
    {
        const string source = """
            class C
            {
                void M()
                {
                    Log("started");
                }

                private void {|ECO005:Log|}(string text) => System.Console.WriteLine(text);
            }
            """;

        const string fixedSource = """
            class C
            {
                void M()
                {
                    System.Console.WriteLine("started");
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task TwoIndependentMethods_AreBothInlined()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var doubled = Twice(value);
                    var tripled = Thrice(value);
                    return doubled + tripled;
                }

                private int {|ECO005:Twice|}(int number) => number * 2;

                private int {|ECO005:Thrice|}(int number) => number * 3;
            }
            """;

        const string fixedSource = """
            class C
            {
                int M(int value)
                {
                    var doubled = value * 2;
                    var tripled = value * 3;
                    return doubled + tripled;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task CallFromNestedType_IsFlagged()
    {
        const string source = """
            class C
            {
                private static int {|ECO005:Twice|}(int number) => number * 2;

                class Inner
                {
                    int M(int value)
                    {
                        var result = Twice(value);
                        return result;
                    }
                }
            }
            """;

        const string fixedSource = """
            class C
            {
                class Inner
                {
                    int M(int value)
                    {
                        var result = value * 2;
                        return result;
                    }
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task TwoCallSites_IsNotFlagged()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var first = Twice(value);
                    var second = Twice(first);
                    return second;
                }

                private int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task CallsAcrossPartialFilesAndNestedTypes_AreCounted()
    {
        const string first = """
            partial class C
            {
                int M(int value)
                {
                    var result = Twice(value);
                    return result;
                }

                private static int Twice(int number) => number * 2;
            }
            """;

        const string second = """
            partial class C
            {
                class Inner
                {
                    int N(int value)
                    {
                        var result = Twice(value);
                        return result;
                    }
                }
            }
            """;

        var test = new CSharpCodeFixTest<SingleUsePrivateMethodAnalyzer, SingleUsePrivateMethodCodeFixProvider, DefaultVerifier>();
        test.TestState.Sources.Add(first);
        test.TestState.Sources.Add(second);
        await test.RunAsync();
    }

    [Fact]
    public async Task RecursiveMethod_IsNotFlagged()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var result = Countdown(value);
                    return result;
                }

                private int Countdown(int number)
                {
                    if (number == 0)
                        return 0;

                    return Countdown(number - 1);
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task UnreferencedMethod_IsNotFlagged()
    {
        const string source = """
            class C
            {
                private int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task MethodGroupReference_IsNotFlagged()
    {
        const string source = """
            using System;

            class C
            {
                void M()
                {
                    Func<int, int> callback = Twice;
                    callback(1);
                }

                private int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NameOfReference_IsNotFlagged()
    {
        const string source = """
            class C
            {
                string M()
                {
                    return nameof(Twice);
                }

                private int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AttributedMethod_IsNotFlagged()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var result = Twice(value);
                    return result;
                }

                [System.Obsolete]
                private int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PartialMethod_IsNotFlagged()
    {
        const string source = """
            partial class C
            {
                void M()
                {
                    Hook();
                }

                partial void Hook();
            }

            partial class C
            {
                partial void Hook()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ExplicitInterfaceImplementation_IsNotFlagged()
    {
        const string source = """
            interface IWorker
            {
                void Work();
            }

            class C : IWorker
            {
                void M()
                {
                    ((IWorker)this).Work();
                }

                void IWorker.Work()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GenericMethod_IsNotFlagged()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    var result = Identity(value);
                    return result;
                }

                private T Identity<T>(T value) => value;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RefLikeAndParamsParameters_AreNotFlagged()
    {
        const string source = """
            class C
            {
                void M(int value)
                {
                    Read(out var first);
                    Bump(ref value);
                    Peek(in value);
                    Sum(1, 2, 3);
                }

                private void Read(out int number)
                {
                    number = 1;
                }

                private void Bump(ref int number)
                {
                    number++;
                }

                private int Peek(in int number) => number;

                private int Sum(params int[] numbers) => numbers.Length;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NonPrivateAndLocalFunctions_AreNotFlagged()
    {
        const string source = """
            class C
            {
                int M(int value)
                {
                    int Local(int number) => number * 2;
                    var result = Local(value);
                    return Twice(result);
                }

                internal int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task BlockWithReturn_HasNoFix()
    {
        const string source = """
            class C
            {
                void M(int value)
                {
                    Log(value);
                }

                private void {|ECO005:Log|}(int number)
                {
                    if (number == 0)
                        return;

                    System.Console.WriteLine(number);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task IteratorMethod_HasNoFix()
    {
        const string source = """
            using System.Collections.Generic;

            class C
            {
                IEnumerable<int> M()
                {
                    return Numbers();
                }

                private IEnumerable<int> {|ECO005:Numbers|}()
                {
                    yield return 1;
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task BodyLocalAlreadyInCaller_HasNoFix()
    {
        const string source = """
            class C
            {
                void M()
                {
                    var text = "outer";
                    Log();
                    System.Console.WriteLine(text);
                }

                private void {|ECO005:Log|}()
                {
                    var text = "inner";
                    System.Console.WriteLine(text);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task UnawaitedTaskStatementBody_HasNoFix()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                void M()
                {
                    Save();
                }

                private async Task {|ECO005:Save|}()
                {
                    await Task.Delay(1);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task RepeatedParameterWithCallArgument_HasNoFix()
    {
        const string source = """
            class C
            {
                int M()
                {
                    var result = Square(Next());
                    return result;
                }

                internal int Next() => 1;

                private int {|ECO005:Square|}(int number) => number * number;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task ArgumentNeedingConversion_HasNoFix()
    {
        const string source = """
            class C
            {
                long M()
                {
                    var result = Twice(1);
                    return result;
                }

                private long {|ECO005:Twice|}(long number) => number * 2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CallOnOtherInstance_IsFlaggedWithoutFix()
    {
        const string source = """
            class C
            {
                int M(C other)
                {
                    var result = other.Twice(1);
                    return result;
                }

                private int {|ECO005:Twice|}(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CallerLocalDeclaredAfterCallSite_HasNoFix()
    {
        const string source = """
            class C
            {
                int count;

                int M()
                {
                    var result = Count();
                    var count = 5;
                    return result + count;
                }

                private int {|ECO005:Count|}() => count + 1;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CallerLocalShadowingBodyField_HasNoFix()
    {
        const string source = """
            class C
            {
                int count;

                int M()
                {
                    var count = 5;
                    var result = Count();
                    return result + count;
                }

                private int {|ECO005:Count|}() => count + 1;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }
}
