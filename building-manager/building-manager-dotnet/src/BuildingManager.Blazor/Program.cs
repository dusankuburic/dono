using BuildingManager.Blazor;
using BuildingManager.Blazor.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API is a separate application; prefer the configured base URL and fall
// back to this app's origin (e.g. when served from the API host in production).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var httpClientBaseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri(apiBaseUrl);

builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped(sp => new HttpClient(sp.GetRequiredService<AuthorizationMessageHandler>())
{
    BaseAddress = httpClientBaseAddress
});

builder.Services.AddMudServices();

builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IBuildingSelectionService, BuildingSelectionService>();

await builder.Build().RunAsync();
