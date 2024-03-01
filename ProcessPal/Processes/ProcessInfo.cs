using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProcessPal.Processes;

class ProcessInfo
{
    private Process _process;
    private CancellationTokenSource _processCancellationSource = new();

    private readonly string _groupName;
    private readonly ProcessConfig _config;
    private readonly object _locker = new();
        
    public ProcessInfo(string groupName, ProcessConfig config)
    {
        _groupName = groupName;
        _config = config;
    }

    public bool IsRunning => _process != null;

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
                var process = _process;
                process.Exited -= OnProcessExited;
                process.Kill(entireProcessTree: true);
                _processCancellationSource.Cancel();
                _process = null;
            }
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "/bin/bash",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"-File {_config.ScriptPath} {_config.Args}" : $"{_config.ScriptPath} {_config.Args}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _process = new Process { StartInfo = startInfo };
                _process.EnableRaisingEvents = true;
                _process.Exited += OnProcessExited;
                _process.Start();

                var cts = _processCancellationSource = new CancellationTokenSource();

                StartForwardingOutput(_process.StandardOutput, Console.Out, cts);
                StartForwardingOutput(_process.StandardError, Console.Error, cts);
            }
        }
    }

    private void StartForwardingOutput(StreamReader reader, TextWriter writer, CancellationTokenSource cts)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var processLine = await reader.ReadLineAsync(cts.Token);
                    var line = $"{_groupName} | {processLine}";
                    await writer.WriteLineAsync(line);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        });
    }

    private void OnProcessExited(object sender, EventArgs e)
    {
        if (_config.AutoRestart)
        {
            lock (_locker)
            {
                Stop();
                Start();
            }
        }
    }
}