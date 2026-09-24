using System.Text;
using BuildingManager.Api.Controllers;
using BuildingManager.Api.Middleware;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Console sink comes from appsettings.json (Serilog:WriteTo) — do not add a
// second one here or every line is logged twice.
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

builder.Services.AddHealthChecks();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
if (Encoding.UTF8.GetBytes(jwtSecret).Length < 32)
    throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep raw claim names (sub, email, role, jti)
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
            // Check the session grain so terminated (logged-out) tokens are
            // rejected immediately instead of staying valid until expiry.
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
                    // Session storage unavailable: fail open rather than lock
                    // everyone out (signature and expiry were already checked).
                    var logger = context.HttpContext.RequestServices
                        .GetService<ILoggerFactory>()?.CreateLogger("JwtSessionValidation");
                    logger?.LogWarning(ex, "Could not validate session {SessionId}", sessionId);
                }
            }
        };
    });
builder.Services.AddAuthorization();

// The Blazor WebAssembly client is hosted on a different origin than this API.
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
            // No origins configured: allow any origin outside production.
            policy.SetIsOriginAllowed(_ => !builder.Environment.IsProduction());
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var clusterId = builder.Configuration["Orleans:ClusterId"] ?? "building-manager-dev";
var serviceId = builder.Configuration["Orleans:ServiceId"] ?? "building-manager-service";

builder.Host.UseOrleansClient(client =>
{
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = clusterId;
        options.ServiceId = serviceId;
    });

    var gatewayPort = builder.Configuration.GetValue<int>("Orleans:GatewayPort", 30000);
    client.UseStaticClustering(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, gatewayPort));
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("Starting Building Manager API...");
Log.Information("Cluster: {ClusterId}, Service: {ServiceId}", clusterId, serviceId);

app.Run();
