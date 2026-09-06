using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EcoData.Analyzers.Tests;

public class NoInjectAttributeTests
{
    // The attribute is matched by metadata name, so the tests carry their own stub of it.
    private const string InjectStub = """
        namespace Microsoft.AspNetCore.Components
        {
            [System.AttributeUsage(System.AttributeTargets.Property)]
            public sealed class InjectAttribute : System.Attribute { }
        }
        """;

    [Fact]
    public async Task PrivateProperty_OnComponent_IsFlagged()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public partial class SpeciesPage
            {
                {|ECO011:[Inject]|}
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ProtectedProperty_OnComponent_IsFlagged()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public partial class SpeciesPage
            {
                {|ECO011:[Inject]|}
                protected object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task PublicProperty_OnComponent_IsFlagged()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public class SpeciesPage
            {
                {|ECO011:[Inject]|}
                public object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task FullyQualifiedAttribute_IsFlagged()
    {
        const string source = """
            public class SpeciesPage
            {
                {|ECO011:[Microsoft.AspNetCore.Components.Inject]|}
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AttributeWithSuffix_IsFlagged()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public class SpeciesPage
            {
                {|ECO011:[InjectAttribute]|}
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task EveryInjectedProperty_IsFlaggedSeparately()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public partial class SpeciesPage
            {
                {|ECO011:[Inject]|}
                private object Client { get; set; } = default!;

                {|ECO011:[Inject]|}
                private object Snackbar { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SealedComponent_IsFlagged()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public sealed class SpeciesPage
            {
                {|ECO011:[Inject]|}
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task AbstractBaseClass_IsNotFlagged()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public abstract class EcoDataComponent
            {
                [Inject]
                protected object L { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task ConcreteSubclassOfAbstractBase_IsFlagged()
    {
        const string source = """
            using Microsoft.AspNetCore.Components;

            public abstract class EcoDataComponent
            {
                [Inject]
                protected object L { get; set; } = default!;
            }

            public class SpeciesPage : EcoDataComponent
            {
                {|ECO011:[Inject]|}
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task SameNamedAttributeFromAnotherNamespace_IsNotFlagged()
    {
        const string source = """
            namespace Other
            {
                [System.AttributeUsage(System.AttributeTargets.Property)]
                public sealed class InjectAttribute : System.Attribute { }
            }

            public class SpeciesPage
            {
                [Other.Inject]
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task OtherAttributes_AreNotFlagged()
    {
        const string source = """
            public class SpeciesPage
            {
                [System.Obsolete]
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    [Fact]
    public async Task PropertyWithoutAttribute_IsNotFlagged()
    {
        const string source = """
            public class SpeciesPage
            {
                private object Client { get; set; } = default!;
            }
            """;

        await VerifyAsync(source);
    }

    private static async Task VerifyAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<NoInjectAttributeAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Default,
        };
        test.TestState.Sources.Add(InjectStub);
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }
}
