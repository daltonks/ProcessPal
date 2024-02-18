﻿using ProcessPal.Generated;
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
        return await _taskQueue.RunAsync(() => {
            if (_processGroups.TryGetValue(request.Name, out var processGroup))
            {
                processGroup.Toggle();
            }
            
            return new ToggleProcessGroupResponse();
        });
    }

    public void StopAllGroups()
    {
        foreach (var group in _processGroups.Values)
        {
            group.Stop();
        }
    }
}