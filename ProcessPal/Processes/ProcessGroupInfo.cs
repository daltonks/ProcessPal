namespace ProcessPal.Processes;

class ProcessGroupInfo
{
    private readonly ProcessInfo[] _processes;
    private bool AnyProcessesRunning => _processes.Any(x => x.IsRunning);
    
    public ProcessGroupInfo(ProcessConfig[] processConfigs)
    {
        _processes = processConfigs
            .Select(x => new ProcessInfo(x))
            .ToArray();
    }

    public void Start()
    {
        foreach (var process in _processes)
        {
            process.Start();
        }
    }

    public void Stop()
    {
        foreach (var process in _processes)
        {
            process.Stop();
        }
    }
    
    public void Toggle()
    {
        if (AnyProcessesRunning)
        {
            Stop();
        }
        else
        {
            Start();
        }
    }
}