using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProcessPal.Processes;

internal class ProcessManager(
    ScriptConfig config, 
    ConsoleColor nameForeground,
    ConsoleColor nameBackground, 
    int namePadRight,
    SemaphoreSlim outputLock
)
{
    private Process _process;
    private CancellationTokenSource _processCancellationSource = new();

    private readonly object _runningLock = new();

    public bool IsRunning => _process != null;

    public void Start()
    {
        lock (_runningLock)
        {
            if (!IsRunning)
            {
                Toggle();
            }
        }
    }

    public void Stop()
    {
        lock (_runningLock)
        {
            if (IsRunning)
            {
                Toggle();
            }
        }
    }
        
    public void Toggle()
    {
        lock (_runningLock)
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
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"-File {config.Path} {config.Args}" : $"{config.Path} {config.Args}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _process = new Process { StartInfo = startInfo };
                _process.EnableRaisingEvents = true;
                _process.Exited += OnProcessExited;
                _process.Start();

                var cts = _processCancellationSource = new CancellationTokenSource();

                StartForwardingOutput(_process.StandardOutput, isError: false, cts);
                StartForwardingOutput(_process.StandardError, isError: true, cts);
            }
        }
    }

    private void StartForwardingOutput(StreamReader reader, bool isError, CancellationTokenSource cts)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var processLine = await reader.ReadLineAsync(cts.Token);

                    await outputLock.WaitAsync();
                    try
                    {
                        Console.BackgroundColor = nameBackground;
                        Console.ForegroundColor = nameForeground;
                        Console.Write($"{config.Name.PadRight(namePadRight)} |");

                        Console.ResetColor();
                        if (isError)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }

                        Console.WriteLine($" {processLine}");
                        if (isError)
                        {
                            Console.ResetColor();
                        }
                    }
                    finally
                    {
                        outputLock.Release();
                    }
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
        if (config.AutoRestart)
        {
            lock (_runningLock)
            {
                Stop();
                Start();
            }
        }
    }
}