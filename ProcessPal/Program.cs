using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using CommandLine;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ProcessPal.Generated;
using ProcessPal.Processes;

namespace ProcessPal;

internal class Program
{
    private static Config _config;
    
    public static int Main(string[] args)
    {
        using (var fileStream = File.OpenRead("_config.json"))
        {
            _config = JsonSerializer.Deserialize<Config>(fileStream);
        }

        return Parse(args);
    }

    private static int Parse(IEnumerable<string> args)
    {
        var result = 0;
        
        var parserResult = Parser.Default.ParseArguments(args, typeof(ToggleServerOptions), typeof(ToggleGroupOptions));

        parserResult.WithParsed<ToggleServerOptions>(options =>
        {
            result = ToggleServer() ? 0 : 1;
        });

        parserResult.WithParsed<ToggleGroupOptions>(options =>
        {
            result = ToggleProcessGroup(options) ? 0 : 1;
        });

        return result;
    }
    
    private static bool ToggleServer()
    {
        var portIsInUse = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            .Any(x => x.Port == _config.Port);
        if (portIsInUse)
        {
            // Shutdown server
            try
            {
                SendToServer(client => client.Shutdown(new ShutdownRequest()), handleExceptions: false);
                Console.WriteLine("The server is shutting down");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Port {_config.Port} is in use, but the server is not responding. " +
                                        $"Maybe the port is being used by another process. " +
                                        $"Try changing Port in _config.json.");
                return false;
            }
        }

        // Start server
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Listen(IPAddress.Loopback, _config.Port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        var services = builder.Services;
        services.AddGrpc();
        services.AddSingleton(_config);
        services.AddSingleton<ProcessService>();

        var app = builder.Build();
        app.MapGrpcService<ProcessControllerImpl>();

        app.Run();

        return true;
    }

    private static bool ToggleProcessGroup(ToggleGroupOptions options)
    {
        var toggleRequest = new ToggleProcessGroupRequest { Name = options.Name };
        return SendToServer(client =>
        {
            var response = client.ToggleProcessGroup(toggleRequest);
            switch (response.Status)
            {
                case ToggleProcessGroupStatus.Started:
                    Console.WriteLine($"Started process group \"{options.Name}\"");
                    break;
                case ToggleProcessGroupStatus.Stopped:
                    Console.WriteLine($"Stopped process group \"{options.Name}\"");
                    break;
                case ToggleProcessGroupStatus.NotFound:
                    Console.WriteLine($"Couldn't find process group \"{options.Name}\"");
                    break;
            }
        });
    }
    
    private static bool SendToServer(Action<ProcessController.ProcessControllerClient> action, bool handleExceptions = true)
    {
        try
        {
            using var channel = GrpcChannel.ForAddress($"http://localhost:{_config.Port}");
            var client = new ProcessController.ProcessControllerClient(channel);
            action(client);
            return true;
        }
        catch(Exception) when (handleExceptions)
        {
            Console.Error.WriteLine($"Couldn't connect to the server at port {_config.Port}. " +
                                    $"If it's not running, start it with the `toggle-server` command. " +
                                    $"If that doesn't work, maybe the port is being used by another process. " +
                                    $"Try changing Port in _config.json.");

            return false;
        }
    }
}