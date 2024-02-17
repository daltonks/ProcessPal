using Grpc.Core;

namespace ProcessPal.Server.Processes;

public class ProcessControllerImpl : ProcessController.ProcessControllerBase
{
    private readonly ILogger<ProcessControllerImpl> _logger;
    private readonly ProcessService _processService;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public ProcessControllerImpl(
        ILogger<ProcessControllerImpl> logger, 
        ProcessService processService,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _processService = processService;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public override async Task<ToggleProcessGroupResponse> ToggleProcessGroup(
        ToggleProcessGroupRequest request, 
        ServerCallContext context)
    {
        return await _processService.ToggleProcessGroupAsync(request);
    }

    public override Task<ShutdownResponse> Shutdown(ShutdownRequest request, ServerCallContext context)
    {
        _hostApplicationLifetime.StopApplication();
        return Task.FromResult(new ShutdownResponse());
    }
}
