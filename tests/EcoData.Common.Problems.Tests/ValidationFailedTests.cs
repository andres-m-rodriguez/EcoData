using Xunit;

namespace EcoData.Common.Problems.Tests;

public class ValidationFailedTests
{
    [Fact]
    public void AllMessages_FlattensEveryFieldInOrder()
    {
        var failed = new ValidationFailed(new Dictionary<string, string[]>
        {
            ["email"] = ["Email is required.", "Email is invalid."],
            ["name"] = ["Name is required."],
        });

        Assert.Equal(["Email is required.", "Email is invalid.", "Name is required."], failed.AllMessages);
    }

    [Fact]
    public void AllMessages_NoErrors_IsEmpty()
    {
        var failed = new ValidationFailed(new Dictionary<string, string[]>());

        Assert.Empty(failed.AllMessages);
    }
}
