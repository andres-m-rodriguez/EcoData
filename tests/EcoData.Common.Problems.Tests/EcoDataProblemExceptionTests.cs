using Xunit;

namespace EcoData.Common.Problems.Tests;

public class EcoDataProblemExceptionTests
{
    [Fact]
    public void Constructor_NullProblem_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EcoDataProblemException(null!));
    }

    [Fact]
    public void Message_UsesProblemTitle()
    {
        var exception = new EcoDataProblemException(EcoDataProblemDetails.NotFound());

        Assert.Equal("The requested resource was not found.", exception.Message);
    }

    [Fact]
    public void Message_TitleMissing_UsesFallback()
    {
        var exception = new EcoDataProblemException(new EcoDataProblemDetails());

        var messageIsBlank = string.IsNullOrWhiteSpace(exception.Message);
        Assert.False(messageIsBlank);
    }
}
