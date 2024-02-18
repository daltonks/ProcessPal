using System.Diagnostics;

namespace ProcessPal.Processes;

class ProcessInfo
{
    private readonly ProcessConfig _config;
    private readonly object _locker = new();
        
    public ProcessInfo(ProcessConfig config)
    {
        _config = config;
    }
        
    public Process Process { get; private set; }
    public bool IsRunning => Process != null;

    public void Start()
    {
        lock (_locker)
        {
            if (!IsRunning)
            {
                Toggle();
            }
        }
    }

    public void Stop()
    {
        lock (_locker)
        {
            if (IsRunning)
            {
                Toggle();
            }
        }
    }
        
    public void Toggle()
    {
        lock (_locker)
        {
            if (IsRunning)
            {
                Process.Exited -= OnProcessExited;
                Process.Kill(entireProcessTree: true);
                Process = null;
            }
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _config.FileName,
                    Arguments = _config.Args,
                    UseShellExecute = true
                };

                Process = Process.Start(startInfo);
                Process!.EnableRaisingEvents = true;
                Process.Exited += OnProcessExited;
            }
        }
    }

    private void OnProcessExited(object sender, EventArgs e)
    {
        if (_config.RestartOnExit)
        {
            lock (_locker)
            {
                Stop();
                Start();
            }
        }
    }
}