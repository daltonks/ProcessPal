using System.Diagnostics;

namespace ProcessPal.Server.Processes;

class ProcessInfo
{
    private readonly ProcessConfig _config;
    private readonly object _locker = new();
        
    public ProcessInfo(ProcessConfig config)
    {
        _config = config;
    }
        
    public Process Process { get; private set; }

    public void Start()
    {
        lock (_locker)
        {
            if (Process == null)
            {
                Toggle();
            }
        }
    }

    public void Stop()
    {
        lock (_locker)
        {
            if (Process != null)
            {
                Toggle();
            }
        }
    }
        
    public void Toggle()
    {
        lock (_locker)
        {
            if (Process == null)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _config.FileName,
                    Arguments = _config.Args,
                    UseShellExecute = true
                };

                Process = Process.Start(startInfo);
                Process!.EnableRaisingEvents = true;
                Process.Exited += OnProcessExited;
            }
            else
            {
                Process.Exited -= OnProcessExited;
                Process.Kill(entireProcessTree: true);
                Process = null;
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