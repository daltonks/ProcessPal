using ProcessPal.Generated;
using ProcessPal.Util;

namespace ProcessPal.Processes;

public class ProcessService
{
    private readonly Dictionary<string, ProcessGroupInfo> _processGroups;
    private readonly TaskQueue _taskQueue = new();

    public ProcessService(Config config)
    {
        _processGroups = config.ProcessGroups?.ToDictionary(x => x.Key, x => new ProcessGroupInfo(x.Value)) 
                         ?? new Dictionary<string, ProcessGroupInfo>();
    }

    public async Task<ToggleProcessGroupResponse> ToggleProcessGroupAsync(ToggleProcessGroupRequest request)
    {
        return await _taskQueue.RunAsync(() =>
        {
            var response = new ToggleProcessGroupResponse();
            
            if (_processGroups.TryGetValue(request.Name, out var processGroup))
            {
                if(processGroup.AnyProcessesRunning)
                {
                    processGroup.Stop();
                    response.Status = ToggleProcessGroupStatus.Stopped;
                }
                else
                {
                    processGroup.Start();
                    response.Status = ToggleProcessGroupStatus.Started;
                }
            }
            else
            {
                response.Status = ToggleProcessGroupStatus.NotFound;
            }
            
            return response;
        });
    }

    public async Task StopAllGroupsAsync()
    {
        await _taskQueue.RunAsync(() =>
        {
            foreach (var group in _processGroups.Values)
            {
                group.Stop();
            }
        });
    }
}