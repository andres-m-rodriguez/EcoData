using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace EcoData.Analyzers.Tests;

public class DataRecordStaysDataTests
{
    // Records need IsExternalInit and required needs RequiredMemberAttribute, neither of which the
    // default netstandard2.0 reference set carries.
    private static Task VerifyAnalyzerAsync(string source)
    {
        var test = new CSharpCodeFixTest<DataRecordStaysDataAnalyzer, EmptyCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        return test.RunAsync();
    }

    [Fact]
    public async Task ExpressionBodiedProperty_InContractsNamespace_IsFlagged()
    {
        const string source = """
            namespace EcoData.Wildlife.Contracts;

            public sealed record SpeciesDto(string Name, int Count)
            {
                public bool {|ECO008:IsEmpty|} => Count == 0;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GetterWithBody_IsFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Contracts.Dtos;

            public sealed record SensorDto(string Name)
            {
                public string {|ECO008:DisplayName|}
                {
                    get { return Name.ToUpperInvariant(); }
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InitAccessorWithBody_IsFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Contracts.Dtos;

            public sealed record SensorDto
            {
                private string _name = "";

                public string {|ECO008:Name|}
                {
                    get => _name;
                    init => _name = value.Trim();
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task InstanceMethod_IsFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Contracts.Dtos;

            public sealed record ReadingDto(double Value, string Unit)
            {
                public string {|ECO008:Format|}()
                {
                    return Value + " " + Unit;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ToStringOverride_IsFlagged()
    {
        const string source = """
            namespace EcoData.Wildlife.Contracts;

            public sealed record SpeciesDto(string Name)
            {
                public override string {|ECO008:ToString|}() => Name;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ExplicitInterfaceImplementation_IsFlagged()
    {
        const string source = """
            namespace EcoData.Wildlife.Contracts;

            public interface INamed
            {
                string Label { get; }
                int Rank();
            }

            public sealed record SpeciesDto(string Name) : INamed
            {
                string INamed.{|ECO008:Label|} => Name;

                int INamed.{|ECO008:Rank|}() => 1;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task Indexer_IsFlagged()
    {
        const string source = """
            using System.Collections.Generic;

            namespace EcoData.Wildlife.Contracts;

            public sealed record BatchDto(IReadOnlyList<string> Items)
            {
                public string {|ECO008:this|}[int index] => Items[index];
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RecordStruct_InDtoNamespace_IsFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Dto;

            public readonly record struct Point(double X, double Y)
            {
                public double {|ECO008:Length|} => X * X + Y * Y;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NestedRecord_IsFlagged()
    {
        const string source = """
            namespace EcoData.Common.Contract;

            public sealed record OuterDto(string Name)
            {
                public sealed record InnerDto(int Count)
                {
                    public bool {|ECO008:IsEmpty|} => Count == 0;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PositionalRecord_NotFlagged()
    {
        const string source = """
            namespace EcoData.Common.Problems.Contracts;

            public sealed record RequestFailed(int StatusCode, string? Message = null);
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task AutoPropertiesAndFields_NotFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Contracts.Dtos;

            public sealed record SensorDto
            {
                private readonly int _revision;

                public required string Name { get; init; }

                public int Count { get; set; }

                public string Unit { get; init; } = "";

                public string Label { get; }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StaticMembers_NotFlagged()
    {
        const string source = """
            namespace EcoData.Common.Problems.Contracts;

            public sealed record RequestFailed(int StatusCode, string? Message = null)
            {
                public static RequestFailed Empty { get; } = new(0);

                public static RequestFailed From(int statusCode)
                {
                    return new RequestFailed(statusCode);
                }

                public static string Describe(RequestFailed failed) => failed.StatusCode.ToString();
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ConstructorsOperatorsAndDeconstruct_NotFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Contracts.Dtos;

            public sealed record Measurement(double Value, string Unit)
            {
                public Measurement(double value) : this(value, "")
                {
                }

                public static Measurement operator +(Measurement left, Measurement right) => new(left.Value + right.Value, left.Unit);

                public static implicit operator double(Measurement measurement) => measurement.Value;

                public void Deconstruct(out double value)
                {
                    value = Value;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task PlainClassAndStruct_InContractsNamespace_NotFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Contracts.Dtos;

            public sealed class SensorSummary
            {
                public int Count { get; set; }

                public bool IsEmpty => Count == 0;
            }

            public struct Range
            {
                public int Start;
                public int End;

                public int Length => End - Start;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RecordOutsideContractNamespace_NotFlagged()
    {
        const string source = """
            namespace EcoData.Sensors.Internal.Contractors;

            public sealed record SensorState(int Count)
            {
                public bool IsEmpty => Count == 0;

                public override string ToString() => Count.ToString();
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task NestedTypeDeclarations_NotFlaggedThemselves()
    {
        const string source = """
            namespace EcoData.Wildlife.Contracts;

            public sealed record SpeciesDto(string Name)
            {
                public enum Status
                {
                    Active,
                    Extinct
                }

                public sealed class Helper
                {
                    public bool IsEmpty(SpeciesDto dto) => dto.Name.Length == 0;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task GeneratedCode_NotFlagged()
    {
        const string source = """
            // <auto-generated/>
            namespace EcoData.Wildlife.Contracts;

            public sealed record SpeciesDto(string Name)
            {
                public bool IsEmpty => Name.Length == 0;
            }
            """;

        await VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task SeveralLogicMembers_EachFlagged()
    {
        const string source = """
            namespace EcoData.Wildlife.Contracts;

            public sealed record SpeciesDto(string Name, int Count)
            {
                public bool {|ECO008:IsEmpty|} => Count == 0;

                public string {|ECO008:DisplayName|} => Name.ToUpperInvariant();

                public int {|ECO008:ActiveCount|}()
                {
                    return Count;
                }
            }
            """;

        await VerifyAnalyzerAsync(source);
    }
}
