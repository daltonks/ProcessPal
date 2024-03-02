using Grpc.Core;
using ProcessPal.Generated;

namespace ProcessPal.Processes;

public class ProcessGroupControllerImpl(
    ProcessGroupService processGroupService,
    IHostApplicationLifetime hostApplicationLifetime
) : ProcessGroupController.ProcessGroupControllerBase
{
    public override Task<ShutdownResponse> Shutdown(ShutdownRequest request, ServerCallContext context)
    {
        processGroupService.Stop();
        hostApplicationLifetime.StopApplication();
        return Task.FromResult(new ShutdownResponse());
    }
}
