using System.Net;
using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using VisitorManagement.Web.Authentication;
using VisitorManagement.Web.Models.Auth;

namespace VisitorManagement.Web.Services;

public sealed class ApiAuthorizationHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    private readonly JwtAuthenticationStateProvider
        _authenticationStateProvider;

    private readonly NavigationManager _navigationManager;

    public ApiAuthorizationHandler(
        ILocalStorageService localStorage,
        JwtAuthenticationStateProvider authenticationStateProvider,
        NavigationManager navigationManager)
    {
        _localStorage = localStorage;
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            Guid.NewGuid().ToString("D"));

        var isLoginRequest =
            request.RequestUri?.AbsolutePath.EndsWith(
                "/api/auth/login",
                StringComparison.OrdinalIgnoreCase) == true;

        var session = await _localStorage
            .GetItemAsync<UserSession>(
                JwtAuthenticationStateProvider.StorageKey,
                cancellationToken);

        if (session is not null)
        {
            if (session.ExpiresAtUtc > DateTime.UtcNow &&
                !string.IsNullOrWhiteSpace(session.AccessToken))
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        session.AccessToken);
            }
            else
            {
                await ClearSessionAsync();
            }
        }

        var response = await base.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            !isLoginRequest)
        {
            await ClearSessionAsync();

            _navigationManager.NavigateTo(
                "/login?expired=true");
        }

        return response;
    }

    private async Task ClearSessionAsync()
    {
        await _localStorage.RemoveItemAsync(
            JwtAuthenticationStateProvider.StorageKey);

        _authenticationStateProvider
            .NotifyUserAuthenticationChanged();
    }
}