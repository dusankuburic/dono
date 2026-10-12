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

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var httpClientBaseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
    ? new Uri(builder.HostEnvironment.BaseAddress)
    : new Uri(apiBaseUrl);

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
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();

await builder.Build().RunAsync();
