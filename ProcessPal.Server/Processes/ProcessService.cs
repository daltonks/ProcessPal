using ProcessPal.Generated;
using ProcessPal.Server.Util;

namespace ProcessPal.Server.Processes;

public class ProcessService : IDisposable
{
    private readonly Dictionary<string, ProcessGroupInfo> _processGroups;
    private readonly TaskQueue _taskQueue = new();

    public ProcessService(Config config)
    {
        _processGroups = config.ProcessGroups?.ToDictionary(x => x.Name, x => new ProcessGroupInfo(x)) 
                         ?? new Dictionary<string, ProcessGroupInfo>();
    }

    public async Task<ToggleProcessGroupResponse> ToggleProcessGroupAsync(ToggleProcessGroupRequest request)
    {
        return await _taskQueue.RunAsync(() => {
            if (_processGroups.TryGetValue(request.Name, out var processGroup))
            {
                processGroup.Toggle();
            }
            
            return new ToggleProcessGroupResponse();
        });
    }

    public void Dispose()
    {
        foreach (var group in _processGroups.Values)
        {
            group.Stop();
        }
    }
}