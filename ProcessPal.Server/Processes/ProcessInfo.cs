using System.Diagnostics;
using System.Runtime.InteropServices;

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
                var scriptPath = Path.Combine(AppContext.BaseDirectory, _config.ScriptName);
                
                ProcessStartInfo startInfo;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-File \"{scriptPath}\" " + _config.Args,
                        UseShellExecute = true
                    };
                }
                else
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"\"{scriptPath}\" " + _config.Args,
                        UseShellExecute = true
                    };
                }

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