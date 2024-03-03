using System.Net;
using System.Net.NetworkInformation;
using CommandLine;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ProcessPal.Generated;
using ProcessPal.Processes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ProcessPal;

internal class Program
{
    private static Config _config;
    
    public static int Main(string[] args)
    {
        var result = 0;
        
        var parserResult = Parser.Default.ParseArguments(args, typeof(ToggleGroupOptions));

        parserResult.WithParsed<ToggleGroupOptions>(options =>
        {
            _config = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Deserialize<Config>(File.ReadAllText(options.ConfigPath));
            result = ToggleProcessGroup(options.Name) ? 0 : 1;
        });

        return result;
    }

    private static bool ToggleProcessGroup(string name)
    {
        if (!_config.TryGetValue(name, out var processGroupConfig))
        {
            Console.Error.WriteLine($"Couldn't find process group \"{name}\"");
            return false;
        }

        var port = processGroupConfig.Port;
        
        var portIsInUse = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            .Any(x => x.Port == port);
        if (portIsInUse)
        {
            // Shutdown server
            try
            {
                Send(port, client => client.Shutdown(new ShutdownRequest()));
                Console.WriteLine($"Stopping process group \"{name}\"");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Console.Error.WriteLine();
                Console.Error.WriteLine($"Port {port} is in use, but the process group server is not responding. " +
                                        $"Maybe the port is being used by another process. " +
                                        $"Try changing its port in _config.yaml.");
                return false;
            }
        }
        
        // Start server
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Listen(IPAddress.Loopback, port, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
        });

        var services = builder.Services;
        services.AddGrpc();
        services.AddSingleton(_config);
        services.AddSingleton<ProcessGroupService>();

        var app = builder.Build();
        app.MapGrpcService<ProcessGroupControllerImpl>();

        var processGroupService = app.Services.GetRequiredService<ProcessGroupService>();
        processGroupService.Start(processGroupConfig);
        
        app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
        {
            processGroupService.Stop();
        });
        
        app.Run();

        return true;
    }
    
    private static void Send(int port, Action<ProcessGroupController.ProcessGroupControllerClient> action)
    {
        using var channel = GrpcChannel.ForAddress($"http://localhost:{port}");
        var client = new ProcessGroupController.ProcessGroupControllerClient(channel);
        action(client);
    }
}