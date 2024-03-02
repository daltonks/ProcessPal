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
    private CancellationTokenSource _outputCancellationSource = new();

    private readonly object _runningLock = new();

    private bool IsRunning => _process != null;

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
                _process.Exited -= OnProcessExited;
                _process.Kill(entireProcessTree: true);

                if (!string.IsNullOrWhiteSpace(config.CleanupScript?.Path))
                {
                    var shutdownProcess = StartProcess(
                        config.CleanupScript.Path, 
                        config.CleanupScript.Args, 
                        config.CleanupScript.EnvPath, 
                        (_, _) => {}, 
                        _outputCancellationSource);
                    shutdownProcess.WaitForExit();
                }
                
                _outputCancellationSource.Cancel();
                
                _process = null;
            }
            else
            {
                _outputCancellationSource = new CancellationTokenSource();
                _process = StartProcess(config.Path, config.Args, config.EnvPath, OnProcessExited, _outputCancellationSource);
            }
        }
    }

    private Process StartProcess(
        string path, 
        string args, 
        string envPath,
        EventHandler exited,
        CancellationTokenSource outputCancellationSource)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "/bin/bash",
            Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"-File {path} {args}" : $"{path} {args}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
                
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            var environmentVariables = File.ReadAllLines(envPath)
                .Select(l => l.Split('=', 2, StringSplitOptions.TrimEntries))
                .ToDictionary(s => s[0], s => s[1]);
            foreach (var environmentVariable in environmentVariables)
            {
                startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
            }
        }
                
        var process = new Process { StartInfo = startInfo };
        process.EnableRaisingEvents = true;
        process.Exited += exited;
        process.Start();

        StartForwardingOutput(process.StandardOutput, isError: false, outputCancellationSource);
        StartForwardingOutput(process.StandardError, isError: true, outputCancellationSource);

        return process;
    }

    private void StartForwardingOutput(StreamReader reader, bool isError, CancellationTokenSource outputCancellationSource)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                while (!outputCancellationSource.IsCancellationRequested)
                {
                    var processLine = await reader.ReadLineAsync(outputCancellationSource.Token);
                    
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