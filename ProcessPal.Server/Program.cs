using System.Net;
using System.Text.Json;
using ProcessPal.Server;
using ProcessPal.Server.Processes;

Config config;
await using (var fileStream = File.OpenRead("config.json"))
{
    config = JsonSerializer.Deserialize<Config>(fileStream);
}

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Loopback, config.Port);
});

services.AddGrpc();
services.AddSingleton<Config>();
services.AddSingleton<ProcessService>();

var app = builder.Build();
app.MapGrpcService<ProcessControllerImpl>();

app.Run();
