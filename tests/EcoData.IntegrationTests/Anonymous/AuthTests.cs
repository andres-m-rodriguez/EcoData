using EcoData.Identity.Application.Client.HttpClients;
using EcoData.Identity.Contracts.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoData.IntegrationTests.Anonymous;

[Collection(EcoDataTestCollection.Name)]
public sealed class AuthTests(EcoDataTestFixture fixture)
{
    IServiceProvider Services => fixture.Services;

    IAuthHttpClient AuthHttpClient => Services.GetRequiredService<IAuthHttpClient>();

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUserInfo()
    {
        var user = await AuthHttpClient.GetCurrentUserAsync();

        user.Should().NotBeNull();
        user!.Email.Should().Be("admin@gmail.com");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsProblemDetail()
    {
        var result = await AuthHttpClient.LoginAsync(
            new LoginRequest("invalid@email.com", "WrongPassword123!")
        );

        result.IsT1.Should().BeTrue("Login with invalid credentials should return ProblemDetail");
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ReturnsProblemDetail()
    {
        var request = new RegisterRequest(
            Email: $"test-{Guid.CreateVersion7():N}@example.com",
            DisplayName: "Test User",
            Password: "ValidPassword123!",
            ConfirmPassword: "DifferentPassword123!"
        );

        var result = await AuthHttpClient.RegisterAsync(request);

        result.IsT1.Should().BeTrue("Registration with mismatched passwords should fail");
        var problem = result.AsT1;
        problem.Status.Should().Be(400);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsProblemDetail()
    {
        var request = new RegisterRequest(
            Email: "not-an-email",
            DisplayName: "Test User",
            Password: "ValidPassword123!",
            ConfirmPassword: "ValidPassword123!"
        );

        var result = await AuthHttpClient.RegisterAsync(request);

        result.IsT1.Should().BeTrue("Registration with invalid email should fail");
        var problem = result.AsT1;
        problem.Status.Should().Be(400);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsProblemDetail()
    {
        var request = new RegisterRequest(
            Email: $"test-{Guid.CreateVersion7():N}@example.com",
            DisplayName: "Test User",
            Password: "short",
            ConfirmPassword: "short"
        );

        var result = await AuthHttpClient.RegisterAsync(request);

        result.IsT1.Should().BeTrue("Registration with short password should fail");
        var problem = result.AsT1;
        problem.Status.Should().Be(400);
    }

    [Fact]
    public async Task Register_AndLogin_Succeeds()
    {
        var email = $"newuser-{Guid.CreateVersion7():N}@example.com";
        var password = "SecurePassword123!";

        var registerRequest = new RegisterRequest(
            Email: email,
            DisplayName: "New Test User",
            Password: password,
            ConfirmPassword: password
        );

        var registerResult = await AuthHttpClient.RegisterAsync(registerRequest);
        registerResult.IsT0.Should().BeTrue("Registration should succeed");

        var registeredUser = registerResult.AsT0;
        registeredUser.Email.Should().Be(email);
        registeredUser.DisplayName.Should().Be("New Test User");
    }
}
