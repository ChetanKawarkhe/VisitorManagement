using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VisitorManagement.Application.DTOs.Auth;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.IntegrationTests;

public sealed class VisitorAuthorizationTests
{
    private const string JwtKey =
        "IntegrationTestSigningKey12345678901234567890";

    private const string JwtIssuer = "VisitorManagement.Tests";
    private const string JwtAudience = "VisitorManagement.Tests";

    private static WebApplicationFactory<Program> CreateTestFactory(
        Mock<IAuthService>? authServiceMock = null)
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=(localdb)\\MSSQLLocalDB;" +
            "Database=VisitorManagementIntegrationTests;" +
            "Trusted_Connection=True;" +
            "TrustServerCertificate=True;");

        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable(
            "Jwt__ExpirationMinutes", "60");

        Environment.SetEnvironmentVariable(
            "AzureStorage__ConnectionString",
            "UseDevelopmentStorage=true");

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

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

    private static string GenerateTestJwt(
        string username,
        string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(JwtKey));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void AddBearerToken(
        HttpClient client,
        string username,
        string role)
    {
        var token = GenerateTestJwt(username, role);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task GetVisitors_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = CreateTestFactory();
        using var client = CreateTestClient(factory);

        var response = await client.GetAsync("/api/visitors");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
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

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            request);

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

    [Fact]
    public async Task GetProfile_WithUserToken_ReturnsOk()
    {
        using var factory = CreateTestFactory();
        using var client = CreateTestClient(factory);

        AddBearerToken(client, "user", "User");

        var response = await client.GetAsync("/api/auth/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Contains("user", responseBody);
        Assert.Contains("User", responseBody);
    }

    [Fact]
    public async Task AdminCheck_WithUserToken_ReturnsForbidden()
    {
        using var factory = CreateTestFactory();
        using var client = CreateTestClient(factory);

        AddBearerToken(client, "user", "User");

        var response = await client.GetAsync("/api/auth/admin-check");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AdminCheck_WithAdminToken_ReturnsOk()
    {
        using var factory = CreateTestFactory();
        using var client = CreateTestClient(factory);

        AddBearerToken(client, "admin", "Admin");

        var response = await client.GetAsync("/api/auth/admin-check");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Admin authorization successful.",
            responseBody);
    }

    [Fact]
    public async Task AdminCheck_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var factory = CreateTestFactory();
        using var client = CreateTestClient(factory);

        var response = await client.GetAsync("/api/auth/admin-check");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}