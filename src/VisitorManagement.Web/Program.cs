using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using VisitorManagement.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register MudBlazor UI services.
builder.Services.AddMudServices();

// Register browser local storage for later JWT authentication.
builder.Services.AddBlazoredLocalStorage();

// HTTP client for the Blazor application itself.
// API-specific HTTP configuration will be added in Part 3.
builder.Services.AddScoped(
    _ => new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

await builder.Build().RunAsync();