using System.Text;
using System.Threading.RateLimiting;
using BuildingManager.Api.Authorization;
using BuildingManager.Api.Controllers;
using BuildingManager.Api.Middleware;
using BuildingManager.Api.Reports;
using BuildingManager.Api.Services;
using BuildingManager.Infrastructure.Storage;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new Azure.Identity.DefaultAzureCredential());
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Building Manager API",
        Version = "v1",
        Description = "API for managing residential buildings"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the JWT from /api/auth/login here."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<OrleansClusterHealthCheck>("orleans-cluster");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
if (Encoding.UTF8.GetBytes(jwtSecret).Length < 32)
    throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "building-manager",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "building-manager-api",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "email",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {

            OnTokenValidated = async context =>
            {
                var sessionId = context.Principal?.FindFirst("jti")?.Value;
                if (string.IsNullOrEmpty(sessionId))
                {
                    return;
                }

                try
                {
                    var grainFactory = context.HttpContext.RequestServices.GetService<IGrainFactory>();
                    if (grainFactory is null)
                    {
                        return;
                    }

                    var sessionGrain = grainFactory.GetGrain<IUserSessionGrain>(sessionId);
                    if (!await sessionGrain.ValidateSessionAsync())
                    {
                        context.Fail("Session has been terminated or expired.");
                    }
                }
                catch (Exception ex)
                {

                    var logger = context.HttpContext.RequestServices
                        .GetService<ILoggerFactory>()?.CreateLogger("JwtSessionValidation");
                    logger?.LogWarning(ex, "Could not validate session {SessionId}", sessionId);
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
{

    options.AddPolicy(AuthPolicies.ManageOnly, policy => policy.RequireRole(AuthPolicies.ManageRoles));

    options.AddPolicy(AuthPolicies.BuildingAccess, policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new BuildingAccessRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, BuildingAccessHandler>();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        if (corsOrigins is { Length: > 0 })
        {
            policy.WithOrigins(corsOrigins);
        }
        else
        {

            policy.SetIsOriginAllowed(_ => !builder.Environment.IsProduction());
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var emailConnection = builder.Configuration["Email:ConnectionString"];
var emailSenderAddress = builder.Configuration["Email:SenderAddress"];
if (!string.IsNullOrEmpty(emailConnection) && !string.IsNullOrEmpty(emailSenderAddress))
{
    builder.Services.AddSingleton<IEmailSender>(new AcsEmailSender(emailConnection, emailSenderAddress));
}
else
{
    builder.Services.AddSingleton<IEmailSender, DevLoggingEmailSender>();
}

var smsConnection = builder.Configuration["Sms:ConnectionString"];
var smsFrom = builder.Configuration["Sms:From"];
if (!string.IsNullOrEmpty(smsConnection) && !string.IsNullOrEmpty(smsFrom))
{
    builder.Services.AddSingleton<ISmsSender>(new AcsSmsSender(smsConnection, smsFrom));
}
else
{
    builder.Services.AddSingleton<ISmsSender, DevLoggingSmsSender>();
}

builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddSingleton<AssemblyReportGenerator>();
builder.Services.AddSingleton<FinanceReportGenerator>();
builder.Services.AddHostedService<AdminSeeder>();

builder.Services.AddSingleton<IBuildingDocumentStorage>(_ =>
{
    var connectionString = builder.Configuration["Storage:BlobConnectionString"]
        ?? throw new InvalidOperationException("Storage:BlobConnectionString is not configured.");
    var containerName = builder.Configuration["Storage:ContainerName"] ?? "building-documents";
    return new AzureBlobDocumentStorage(connectionString, containerName);
});

var authRateLimit = builder.Configuration.GetValue("RateLimit:AuthRequestsPerMinute", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Too many requests",
            Detail = "Too many authentication attempts; please wait a minute and try again."
        }, cancellationToken);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var clusterId = builder.Configuration["Orleans:ClusterId"] ?? "building-manager-dev";
var serviceId = builder.Configuration["Orleans:ServiceId"] ?? "building-manager-service";

builder.Host.UseOrleansClient(client =>
{
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = clusterId;
        options.ServiceId = serviceId;
    });

    var orleansConnectionString = builder.Configuration.GetConnectionString("OrleansStorage");
    if (!string.IsNullOrWhiteSpace(orleansConnectionString))
    {
        client.UseAdoNetClustering(options =>
        {
            options.ConnectionString = orleansConnectionString;
            options.Invariant = "Npgsql";
        });
    }
    else
    {
        var gatewayPort = builder.Configuration.GetValue<int>("Orleans:GatewayPort", 30000);
        client.UseStaticClustering(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, gatewayPort));
    }
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("Starting Building Manager API...");
Log.Information("Cluster: {ClusterId}, Service: {ServiceId}", clusterId, serviceId);

app.Run();
