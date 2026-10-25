using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using VisitorManagement.Web.Models.Auth;

namespace VisitorManagement.Web.Authentication;

public sealed class JwtAuthenticationStateProvider
    : AuthenticationStateProvider
{
    public const string StorageKey = "visitor_management_session";

    private readonly ILocalStorageService _localStorage;

    public JwtAuthenticationStateProvider(
        ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        try
        {
            var session = await _localStorage
                .GetItemAsync<UserSession>(StorageKey);

            if (session is null ||
                string.IsNullOrWhiteSpace(session.AccessToken) ||
                session.ExpiresAtUtc <= DateTime.UtcNow)
            {
                if (session is not null)
                {
                    await _localStorage.RemoveItemAsync(StorageKey);
                }

                return CreateAnonymousState();
            }

            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.Name,
                    session.Username),

                new(
                    ClaimTypes.Role,
                    session.Role)
            };

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Jwt");

            return new AuthenticationState(
                new ClaimsPrincipal(identity));
        }
        catch
        {
            return CreateAnonymousState();
        }
    }

    public void NotifyUserAuthenticationChanged()
    {
        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());
    }

    private static AuthenticationState CreateAnonymousState()
    {
        return new AuthenticationState(
            new ClaimsPrincipal(
                new ClaimsIdentity()));
    }
}