using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    EcoData.Analyzers.BannedTypeNameSuffixAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace EcoData.Analyzers.Tests;

public class BannedTypeNameSuffixTests
{
    [Fact]
    public async Task ClassEndingInService_IsFlagged()
    {
        const string source = """
            class {|ECO007:UserLookupService|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ClassNamedExactlyTheSuffix_IsFlagged()
    {
        const string source = """
            class {|ECO007:Helper|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InterfaceEndingInService_IsFlagged()
    {
        const string source = """
            interface {|ECO007:IUserLookupService|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RecordEndingInManager_IsFlagged()
    {
        const string source = """
            record {|ECO007:SessionManager|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StructEndingInUtils_IsFlagged()
    {
        const string source = """
            struct {|ECO007:DateUtils|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RecordStructEndingInUtilities_IsFlagged()
    {
        const string source = """
            record struct {|ECO007:StringUtilities|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticClassEndingInHelpers_ReportsThePluralSuffix()
    {
        const string source = """
            static class {|#0:PathHelpers|}
            {
            }
            """;

        var expected = VerifyCS.Diagnostic().WithLocation(0).WithArguments("PathHelpers", "Helpers");
        await VerifyCS.VerifyAnalyzerAsync(source, expected);
    }

    [Fact]
    public async Task GenericClassEndingInService_IsFlagged()
    {
        const string source = """
            class {|ECO007:CacheService|}<T>
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NestedClassEndingInCommon_IsFlagged()
    {
        const string source = """
            class Outer
            {
                class {|ECO007:ValidationCommon|}
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PartialClass_IsFlaggedOnEveryPart()
    {
        const string source = """
            partial class {|ECO007:ReportManager|}
            {
            }

            partial class {|ECO007:ReportManager|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SubclassOfSuffixedBase_IsStillFlagged()
    {
        const string source = """
            abstract class {|ECO007:NavigationManager|}
            {
            }

            class {|ECO007:SpaNavigationManager|} : NavigationManager
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task EnumEndingInService_NotFlagged()
    {
        const string source = """
            enum BackgroundService
            {
                Idle,
                Running,
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DelegateEndingInManager_NotFlagged()
    {
        const string source = """
            delegate void LifetimeManager();
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task LowercaseSuffix_NotFlagged()
    {
        const string source = """
            class Microservice
            {
            }

            class Uncommon
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SuffixInTheMiddleOfTheName_NotFlagged()
    {
        const string source = """
            class ServiceRegistration
            {
            }

            interface IManagerDirectory
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NamedForItsResponsibility_NotFlagged()
    {
        const string source = """
            class UserLookup
            {
            }

            interface ISpeciesCatalogue
            {
            }

            record struct Coordinates
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GeneratedCode_NotFlagged()
    {
        const string source = """
            // <auto-generated/>
            class GeneratedService
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
