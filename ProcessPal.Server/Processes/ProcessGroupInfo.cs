namespace ProcessPal.Server.Processes;

class ProcessGroupInfo
{
    private readonly ProcessInfo[] _processes;
    private bool _isRunning;
    
    public ProcessGroupInfo(ProcessGroupConfig config)
    {
        _processes = config.Processes
            .Select(x => new ProcessInfo(x))
            .ToArray();
    }

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }
        
        Toggle();
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }
        
        Toggle();
    }
    
    public void Toggle()
    {
        if (_isRunning)
        {
            foreach (var process in _processes)
            {
                process.Stop();
            }
        }
        else
        {
            foreach (var process in _processes)
            {
                process.Start();
            }
        }

        _isRunning = !_isRunning;
    }
}