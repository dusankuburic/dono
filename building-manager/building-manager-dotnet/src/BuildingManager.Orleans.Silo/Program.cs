using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using OrleansDashboard;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var siloPort = builder.Configuration.GetValue<int>("Orleans:SiloPort", 11111);
var gatewayPort = builder.Configuration.GetValue<int>("Orleans:GatewayPort", 30000);
var dashboardPort = builder.Configuration.GetValue<int>("Orleans:DashboardPort", 8081);
var httpPort = builder.Configuration.GetValue<int>("Orleans:HttpPort", 8080);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(siloPort);
    options.ListenAnyIP(gatewayPort);
    options.ListenAnyIP(httpPort);
});

var connectionString = builder.Configuration.GetConnectionString("OrleansStorage")
    ?? "Host=localhost;Port=5432;Database=building_manager_orleans;Username=postgres;Password=postgres";

var clusterId = builder.Configuration["Orleans:ClusterId"] ?? "building-manager-dev";
var serviceId = builder.Configuration["Orleans:ServiceId"] ?? "building-manager-service";
var advertisedIpAddress = builder.Configuration["Orleans:AdvertisedIPAddress"];

builder.Host.UseOrleans(silo =>
{
    silo
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = clusterId;
            options.ServiceId = serviceId;
        })
        .ConfigureEndpoints(siloPort: siloPort, gatewayPort: gatewayPort);

    if (IPAddress.TryParse(advertisedIpAddress, out var advertisedIp))
    {
        silo.Configure<EndpointOptions>(options => options.AdvertisedIPAddress = advertisedIp);
    }

    silo
        .UseAdoNetClustering(options =>
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
        })
        .UseDashboard(options =>
        {
            options.Host = "*";
            options.HostSelf = true;
            options.Port = dashboardPort;
        });

    silo.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "orleans-storage", tags: new[] { "ready" });
});

var app = builder.Build();

Log.Information("Starting Orleans Silo for Building Manager...");
Log.Information("Cluster: {ClusterId}, Service: {ServiceId}", clusterId, serviceId);
Log.Information("Dashboard available at port {Port}", dashboardPort);

app.MapHealthChecks("/health");

app.Run();
