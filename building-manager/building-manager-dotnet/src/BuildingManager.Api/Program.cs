using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using BuildingManager.Api.Authorization;
using BuildingManager.Api.Controllers;
using BuildingManager.Api.Middleware;
using BuildingManager.Api.Reports;
using BuildingManager.Api.Services;
using BuildingManager.Infrastructure.Storage;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
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

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, _, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext(),
    writeToProviders: true);

if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor()
        .WithTracing(tracing => tracing.AddSource("Microsoft.Orleans.Runtime", "Microsoft.Orleans.Application"))
        .WithMetrics(metrics => metrics.AddMeter("Microsoft.Orleans"));
}

var clusterId = builder.Configuration["Orleans:ClusterId"] ?? "building-manager-dev";
var serviceId = builder.Configuration["Orleans:ServiceId"] ?? "building-manager-service";
var useInMemoryOrleans = builder.Configuration.GetValue("Orleans:UseInMemory", false);
var orleansConnection = builder.Configuration.GetConnectionString("OrleansStorage");
builder.Host.UseOrleans(silo => ConfigureSilo(silo, builder.Configuration));

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

var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck<OrleansClusterHealthCheck>("orleans-cluster", tags: new[] { "ready" });
if (!useInMemoryOrleans && !string.IsNullOrWhiteSpace(orleansConnection))
{
    healthChecks.AddNpgSql(orleansConnection, name: "orleans-storage", tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(5));
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException(
        "Jwt:Secret is not configured. Set it with user secrets or the Jwt__Secret environment variable (see README).");
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
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IEmailSender, DevLoggingEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, UnconfiguredEmailSender>();
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
    var connectionString = builder.Configuration["Storage:BlobConnectionString"];
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Storage:BlobConnectionString is not configured.");
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

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

Log.Information("Starting Building Manager API (cluster {ClusterId}, service {ServiceId}, {Storage} storage)",
    clusterId, serviceId, useInMemoryOrleans ? "in-memory" : "PostgreSQL");

app.Run();

static void ConfigureSilo(ISiloBuilder silo, IConfiguration configuration)
{
    var clusterId = configuration["Orleans:ClusterId"] ?? "building-manager-dev";
    var serviceId = configuration["Orleans:ServiceId"] ?? "building-manager-service";
    var siloPort = configuration.GetValue("Orleans:SiloPort", 11111);
    var gatewayPort = configuration.GetValue("Orleans:GatewayPort", 30000);

    silo.AddActivityPropagation();

    if (configuration.GetValue("Orleans:UseInMemory", false))
    {
        silo.UseLocalhostClustering(siloPort, gatewayPort, serviceId: serviceId, clusterId: clusterId)
            .AddMemoryGrainStorage("buildingStorage")
            .AddReminders()
            .UseInMemoryReminderService();
    }
    else
    {
        var connectionString = configuration.GetConnectionString("OrleansStorage");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:OrleansStorage is not configured. Point it at the Orleans PostgreSQL database " +
                "(schema: infra/orleans-sql), or set Orleans:UseInMemory=true for a throwaway local run.");

        silo.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = clusterId;
            options.ServiceId = serviceId;
        });

        silo.Configure<ClusterMembershipOptions>(options =>
            options.IAmAliveTablePublishTimeout = TimeSpan.FromSeconds(
                configuration.GetValue("Orleans:IAmAlivePublishSeconds", 30)));

        if (IPAddress.TryParse(configuration["Orleans:AdvertisedIPAddress"], out var advertisedIp))
            silo.ConfigureEndpoints(advertisedIp, siloPort, gatewayPort, listenOnAnyHostAddress: true);
        else
            silo.ConfigureEndpoints(siloPort, gatewayPort, listenOnAnyHostAddress: true);

        silo.UseAdoNetClustering(options =>
            {
                options.ConnectionString = connectionString;
                options.Invariant = "Npgsql";
            })
            .AddAdoNetGrainStorage("buildingStorage", options =>
            {
                options.ConnectionString = connectionString;
                options.Invariant = "Npgsql";
            })
            .AddReminders()
            .UseAdoNetReminderService(options =>
            {
                options.ConnectionString = connectionString;
                options.Invariant = "Npgsql";
            });
    }

    silo.AddStartupTask(async (services, _) =>
    {
        try
        {
            await services.GetRequiredService<IGrainFactory>()
                .GetGrain<ISchedulerGrain>("global")
                .EnsureScheduledAsync();
        }
        catch (Exception ex)
        {
            services.GetRequiredService<ILoggerFactory>().CreateLogger("Scheduler")
                .LogError(ex, "Could not start the background job scheduler");
        }
    });

    var dashboardPort = configuration.GetValue("Orleans:DashboardPort", 0);
    if (dashboardPort > 0)
    {
        silo.UseDashboard(options =>
        {
            options.Host = configuration["Orleans:DashboardHost"] ?? "localhost";
            options.HostSelf = true;
            options.Port = dashboardPort;
            options.Username = configuration["Orleans:DashboardUsername"];
            options.Password = configuration["Orleans:DashboardPassword"];
        });
    }
}
