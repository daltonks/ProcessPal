using System.Text.Json;
using CommandLine;
using Grpc.Core;
using Grpc.Net.Client;
using Polly;
using ProcessPal.Client;
using ProcessPal.Generated;

Config config;
await using (var fileStream = File.OpenRead("config.json"))
{
    config = JsonSerializer.Deserialize<Config>(fileStream);
}

using var channel = GrpcChannel.ForAddress($"http://localhost:{config.ServerPort}");
var client = new ProcessController.ProcessControllerClient(channel);

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
            await client.ToggleProcessGroupAsync(toggleRequest);
        });
        
        await parserResult.WithParsedAsync<ShutdownOptions>(async options =>
        {
            try
            {
                await client.ShutdownAsync(new ShutdownRequest());
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