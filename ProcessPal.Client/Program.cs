using System.Text.Json;
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
        switch (args[0])
        {
            case "toggle":
            {
                var toggleRequest = new ToggleProcessGroupRequest { Name = args[1] };
                await client.ToggleProcessGroupAsync(toggleRequest);
                break;
            }
            case "shutdown":
                await client.ShutdownAsync(new ShutdownRequest());
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        throw;
    }
});