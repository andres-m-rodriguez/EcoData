using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.PrivateMethodStaticAnalyzer,
    EcoData.Analyzers.PrivateMethodStaticCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class PrivateMethodStaticTests
{
    [Fact]
    public async Task PrivateInstanceMethod_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                private int {|ECO004:Twice|}(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            class C
            {
                private static int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ImplicitlyPrivateMethod_IsFlaggedAndFixed()
    {
        const string source = """
            class C
            {
                int {|ECO004:Twice|}(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            class C
            {
                static int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task AsyncMethod_KeepsStaticBeforeAsync()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                private async Task {|ECO004:Wait|}(int milliseconds)
                {
                    await Task.Delay(milliseconds);
                }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                private static async Task Wait(int milliseconds)
                {
                    await Task.Delay(milliseconds);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task ImplicitlyPrivateAsyncMethod_PutsStaticFirst()
    {
        const string source = """
            using System.Threading.Tasks;

            class C
            {
                async Task {|ECO004:Wait|}(int milliseconds)
                {
                    await Task.Delay(milliseconds);
                }
            }
            """;

        const string fixedSource = """
            using System.Threading.Tasks;

            class C
            {
                static async Task Wait(int milliseconds)
                {
                    await Task.Delay(milliseconds);
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task MethodUsingStaticMembersOnly_IsFixed()
    {
        const string source = """
            class C
            {
                private static int _count;

                private int {|ECO004:Next|}()
                {
                    _count++;
                    return Twice(_count);
                }

                private static int Twice(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            class C
            {
                private static int _count;

                private static int Next()
                {
                    _count++;
                    return Twice(_count);
                }

                private static int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task MethodReadingAnotherInstance_IsFixed()
    {
        const string source = """
            class C
            {
                public int Count { get; set; }

                private int {|ECO004:CountOf|}(C other) => other.Count;
            }
            """;

        const string fixedSource = """
            class C
            {
                public int Count { get; set; }

                private static int CountOf(C other) => other.Count;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task MethodUsingNameOfInstanceMember_IsFixed()
    {
        const string source = """
            class C
            {
                public int Count { get; set; }

                private string {|ECO004:Label|}() => nameof(Count);
            }
            """;

        const string fixedSource = """
            class C
            {
                public int Count { get; set; }

                private static string Label() => nameof(Count);
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task EmptyBodyWithAttribute_IsFixed()
    {
        const string source = """
            using System;

            class C
            {
                [Obsolete]
                private void {|ECO004:OnChanged|}(int _) { }
            }
            """;

        const string fixedSource = """
            using System;

            class C
            {
                [Obsolete]
                private static void OnChanged(int _) { }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task MethodUsingPrimaryConstructorParameter_IsFlaggedWithoutFix()
    {
        const string source = """
            class C(int seed)
            {
                private int {|ECO004:Next|}() => seed + 1;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task MethodReadingField_IsFlaggedWithoutFix()
    {
        const string source = """
            class C
            {
                private int _count;

                private int {|ECO004:Next|}() => _count + 1;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task MethodUsingThis_IsFlaggedWithoutFix()
    {
        const string source = """
            class C
            {
                private C {|ECO004:Self|}() => this;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task MethodCallingInstanceMethod_IsFlaggedWithoutFix()
    {
        const string source = """
            class C
            {
                public int Count() => 1;

                private int {|ECO004:Next|}() => Count() + 1;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task MethodUsingInheritedMember_IsFlaggedWithoutFix()
    {
        const string source = """
            class Base
            {
                protected int Count { get; set; }
            }

            class C : Base
            {
                private int {|ECO004:Next|}() => Count + 1;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task MethodInStruct_IsFlaggedAndFixed()
    {
        const string source = """
            struct S
            {
                private int {|ECO004:Twice|}(int number) => number * 2;
            }
            """;

        const string fixedSource = """
            struct S
            {
                private static int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Fact]
    public async Task NonPrivateMethods_AreNotFlagged()
    {
        const string source = """
            class C
            {
                public int A(int number) => number;
                protected int B(int number) => number;
                internal int D(int number) => number;
                protected internal int E(int number) => number;
                private protected int F(int number) => number;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticPrivateMethod_IsNotFlagged()
    {
        const string source = """
            class C
            {
                private static int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConstructorsAccessorsAndLocalFunctions_AreNotFlagged()
    {
        const string source = """
            class C
            {
                private C()
                {
                }

                public int Count { get; private set; }

                public int Run()
                {
                    int Twice(int number) => number * 2;
                    return Twice(Count);
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
                int Work(int number);
            }

            class C : IWorker
            {
                int IWorker.Work(int number) => number;
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
                partial void OnChanged(int value);
            }

            partial class C
            {
                partial void OnChanged(int value)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GeneratedCode_IsNotFlagged()
    {
        const string source = """
            // <auto-generated/>
            class C
            {
                private int Twice(int number) => number * 2;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
