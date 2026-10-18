using System.Net.WebSockets;
using OcppGateway.Endpoints;
using OcppGateway.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services
    .AddInfrastructureServices(builder.Configuration)
    .AddExternalServices(builder.Configuration)
    .AddApplicationServices()
    .AddApiServices(builder.Configuration);

var builderApp = builder.Build();

builderApp.UseCors("AllowAll");
builderApp.UseDefaultFiles();
builderApp.UseStaticFiles();

builderApp.UseSwagger();
builderApp.UseSwaggerUI();

var webSocketOptions = new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) };
webSocketOptions.AllowedOrigins.Add("*");
builderApp.UseWebSockets(webSocketOptions);

builderApp.UseAuthentication();
builderApp.UseAuthorization();

builderApp.MapOcppEndpoints();
builderApp.MapCommandEndpoints();
builderApp.MapReportEndpoints();
builderApp.MapUserEndpoints();
builderApp.MapAssetEndpoints();
builderApp.MapTariffEndpoints();
builderApp.MapAuthEndpoints(builder.Configuration);

builderApp.Run();