using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using VisitorManagement.Application.DTOs.Auth;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.IntegrationTests;

public sealed class VisitorAuthorizationTests
{
    private static WebApplicationFactory<Program> CreateTestFactory(
        Mock<IAuthService>? authServiceMock = null)
    {
        // Set configuration before Program.cs starts.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=(localdb)\\MSSQLLocalDB;" +
            "Database=VisitorManagementIntegrationTests;" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;");

        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "IntegrationTestSigningKey12345678901234567890");

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            "VisitorManagement.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            "VisitorManagement.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__ExpirationMinutes",
            "60");

        Environment.SetEnvironmentVariable(
            "AzureStorage__ConnectionString",
            "UseDevelopmentStorage=true");

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Avoid development migrations and database seeding.
                builder.UseEnvironment("Testing");

                // Replace the real authentication service when requested.
                if (authServiceMock is not null)
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IAuthService>();

                        services.AddScoped<IAuthService>(
                            _ => authServiceMock.Object);
                    });
                }
            });
    }

    private static HttpClient CreateTestClient(
        WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task GetVisitors_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        using var factory = CreateTestFactory();
        using var client = CreateTestClient(factory);

        // Act
        var response = await client.GetAsync("/api/visitors");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var authServiceMock = new Mock<IAuthService>();

        authServiceMock
            .Setup(service => service.LoginAsync(
                It.IsAny<LoginRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginResponse?)null);

        using var factory = CreateTestFactory(authServiceMock);
        using var client = CreateTestClient(factory);

        var request = new LoginRequest
        {
            Username = "invaliduser",
            Password = "WrongPassword123"
        };

        // Act
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Invalid username or password. Please verify your credentials and try again.",
            responseBody);

        authServiceMock.Verify(
            service => service.LoginAsync(
                It.Is<LoginRequest>(login =>
                    login.Username == "invaliduser" &&
                    login.Password == "WrongPassword123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}