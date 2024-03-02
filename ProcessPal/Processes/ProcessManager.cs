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
                        (_, _) => {});
                    shutdownProcess.WaitForExit();
                }
                
                _process = null;
            }
            else
            {
                _process = StartProcess(config.Path, config.Args, config.EnvPath, OnProcessExited);
            }
        }
    }

    private Process StartProcess(
        string path, 
        string args, 
        string envPath,
        EventHandler exited)
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
        process.OutputDataReceived += (sender, eventArgs) =>
        {
            ForwardOutput(eventArgs.Data, isError: false);
        };
        process.ErrorDataReceived += (sender, eventArgs) =>
        {
            ForwardOutput(eventArgs.Data, isError: true);
        };
        process.Exited += exited;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private async void ForwardOutput(string line, bool isError)
    {
        if(string.IsNullOrWhiteSpace(line))
        {
            return;
        }
        
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

            Console.WriteLine($" {line}");
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