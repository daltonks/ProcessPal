using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using CommandLine;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Polly;
using ProcessPal;
using ProcessPal.Generated;
using ProcessPal.Processes;

Config config;
await using (var fileStream = File.OpenRead("_config.json"))
{
    config = JsonSerializer.Deserialize<Config>(fileStream);
}

var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(.2));

await retryPolicy.ExecuteAsync(async () =>
{
    try
    {
        var parserResult = Parser.Default.ParseArguments(args, typeof(ToggleOptions), typeof(ShutdownOptions));
        
        await parserResult.WithParsedAsync<ToggleOptions>(async options =>
        {
            var toggleRequest = new ToggleProcessGroupRequest { Name = options.Name };
            await RunAsync(client => client.ToggleProcessGroup(toggleRequest));
        });
        
        await parserResult.WithParsedAsync<ShutdownOptions>(async options =>
        {
            try
            {
                await RunAsync(client => client.Shutdown(new ShutdownRequest()), startServerIfNotRunning: false);
            }
            catch (Exception ex) when (ex is RpcException { StatusCode: StatusCode.Unavailable })
            {
                // Ignore, server is probably already shutdown
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
});

return;

async Task RunAsync(Action<ProcessController.ProcessControllerClient> action, bool startServerIfNotRunning = true)
{
    var runServerTask = Task.CompletedTask;
    if (startServerIfNotRunning)
    {
        var portInUse = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            .Any(x => x.Port == config.Port);

        if (!portInUse)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Listen(IPAddress.Loopback, config.Port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
            });

            var services = builder.Services;
            services.AddGrpc();
            services.AddSingleton(config);
            services.AddSingleton<ProcessService>();

            var app = builder.Build();
            app.MapGrpcService<ProcessControllerImpl>();

            runServerTask = app.RunAsync();
        }
    }
    
    using var channel = GrpcChannel.ForAddress($"http://localhost:{config.Port}");
    var client = new ProcessController.ProcessControllerClient(channel);
    action(client);

    await runServerTask;
}