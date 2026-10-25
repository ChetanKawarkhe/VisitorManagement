using System.Net;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using VisitorManagement.Web.Authentication;
using VisitorManagement.Web.Models.Auth;

namespace VisitorManagement.Web.Services;

public sealed class AuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILocalStorageService _localStorage;

    private readonly JwtAuthenticationStateProvider
        _authenticationStateProvider;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        ILocalStorageService localStorage,
        JwtAuthenticationStateProvider authenticationStateProvider)
    {
        _httpClientFactory = httpClientFactory;
        _localStorage = localStorage;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<LoginResult> LoginAsync(
        string username,
        string password)
    {
        var client = _httpClientFactory.CreateClient(
            "VisitorManagementApi");

        var request = new LoginRequest
        {
            Username = username.Trim(),
            Password = password
        };

        using var response = await client.PostAsJsonAsync(
            "api/auth/login",
            request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return LoginResult.InvalidCredentials();
        }

        if (!response.IsSuccessStatusCode)
        {
            return LoginResult.Failed();
        }

        var loginResponse = await response.Content
            .ReadFromJsonAsync<LoginResponse>();

        if (loginResponse is null ||
            string.IsNullOrWhiteSpace(loginResponse.AccessToken) ||
            loginResponse.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return LoginResult.Failed();
        }

        var session = new UserSession
        {
            AccessToken = loginResponse.AccessToken,
            Username = loginResponse.Username,
            Role = loginResponse.Role,
            ExpiresAtUtc = loginResponse.ExpiresAtUtc
        };

        await _localStorage.SetItemAsync(
            JwtAuthenticationStateProvider.StorageKey,
            session);

        _authenticationStateProvider
            .NotifyUserAuthenticationChanged();

        return LoginResult.Success();
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(
            JwtAuthenticationStateProvider.StorageKey);

        _authenticationStateProvider
            .NotifyUserAuthenticationChanged();
    }
}

public sealed class LoginResult
{
    public bool IsSuccessful { get; private init; }

    public bool AreCredentialsInvalid { get; private init; }

    public static LoginResult Success()
    {
        return new LoginResult
        {
            IsSuccessful = true
        };
    }

    public static LoginResult InvalidCredentials()
    {
        return new LoginResult
        {
            AreCredentialsInvalid = true
        };
    }

    public static LoginResult Failed()
    {
        return new LoginResult();
    }
}