using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ProcessPal.Server;
using ProcessPal.Server.Processes;

Config config;
await using (var fileStream = File.OpenRead("_config.json"))
{
    config = JsonSerializer.Deserialize<Config>(fileStream);
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Loopback, config.Port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
});

var services = builder.Services;
services.AddGrpc();
services.AddSingleton<Config>();
services.AddSingleton<ProcessService>();

var app = builder.Build();
app.MapGrpcService<ProcessControllerImpl>();

app.Run();
