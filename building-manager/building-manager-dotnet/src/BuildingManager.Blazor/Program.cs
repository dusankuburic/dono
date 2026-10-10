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

// Two HttpClients: a plain one for auth calls (no bearer, no auto-refresh —
// avoids recursion) and the API one that attaches tokens and refreshes on 401.
builder.Services.AddKeyedScoped("AuthHttpClient",
    (sp, _) => new HttpClient { BaseAddress = httpClientBaseAddress });
builder.Services.AddKeyedScoped("ApiHttpClient",
    (sp, _) => new HttpClient(sp.GetRequiredService<AuthorizationMessageHandler>())
    {
        BaseAddress = httpClientBaseAddress
    });

builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddMudServices();

builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IBuildingSelectionService, BuildingSelectionService>();
builder.Services.AddScoped<IAssemblyService, AssemblyService>();

await builder.Build().RunAsync();
