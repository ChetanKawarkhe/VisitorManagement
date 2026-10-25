using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using VisitorManagement.Web;
using VisitorManagement.Web.Authentication;
using VisitorManagement.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// MudBlazor.
builder.Services.AddMudServices();

// Browser storage.
builder.Services.AddBlazoredLocalStorage();

// Authentication.
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<
    JwtAuthenticationStateProvider>();

builder.Services.AddScoped<
    AuthenticationStateProvider>(
    serviceProvider =>
        serviceProvider.GetRequiredService<
            JwtAuthenticationStateProvider>());

builder.Services.AddScoped<AuthenticationService>();

// API authorization handler.
builder.Services.AddTransient<ApiAuthorizationHandler>();

// Visitor management services.
builder.Services.AddScoped<LookupApiService>();
builder.Services.AddScoped<VisitorApiService>();

// API configuration.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "API BaseUrl is missing from configuration.");
}

if (!Uri.TryCreate(
        apiBaseUrl,
        UriKind.Absolute,
        out var apiBaseUri))
{
    throw new InvalidOperationException(
        "API BaseUrl must be a valid absolute URL.");
}

// Backend API client.
builder.Services
    .AddHttpClient(
        "VisitorManagementApi",
        client =>
        {
            client.BaseAddress = apiBaseUri;
        })
    .AddHttpMessageHandler<ApiAuthorizationHandler>();

// Default frontend HTTP client.
builder.Services.AddScoped(
    _ => new HttpClient
    {
        BaseAddress =
            new Uri(builder.HostEnvironment.BaseAddress)
    });

await builder.Build().RunAsync();