﻿using System.Diagnostics;
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
    private static readonly string[] NewlineSeparators = { "\r\n", "\r", "\n" };
    
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
                    var shutdownProcess = StartProcess(config.CleanupScript, (_, _) => {});
                    shutdownProcess?.WaitForExit();
                }
                
                _process = null;
            }
            else
            {
                _process = StartProcess(config, OnProcessExited);
            }
        }
    }

    private Process StartProcess(IScriptConfig scriptConfig, EventHandler exited)
    {
        var scriptProvided = !string.IsNullOrWhiteSpace(scriptConfig.Script);
        var pathProvided = !string.IsNullOrWhiteSpace(scriptConfig.Path);
        
        var envPath = scriptConfig.EnvPath;
        var env = scriptConfig.Env;
        var script = scriptConfig.Script;
        var path = scriptConfig.Path;
        var args = scriptConfig.Args;
        
        if (scriptProvided && pathProvided)
        {
            Console.Error.WriteLine($"Error: Both `script` and `path` properties have been provided for script \"{config.Name}\" or its cleanup script. This is not supported.");
            return null;
        }
        
        if (!scriptProvided && !pathProvided)
        {
            Console.Error.WriteLine($"Error: No `script` or `path` properties provided for script \"{config.Name}\" or its cleanup script.");
            return null;
        }
        
        var templatedLines = scriptProvided 
            ? script.Split(NewlineSeparators, StringSplitOptions.None) 
            : File.ReadAllLines(path);
        var lines = ProcessTemplate(templatedLines);
        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".ps1" : ".sh";
        var tempPath = Path.GetTempFileName() + extension;
        File.WriteAllLines(tempPath, lines);
        path = tempPath;

        var arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? $"-File {path} {args}"
            : $"{path} {args}" ;
        
        var startInfo = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "/bin/bash",
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(config.WorkDir) ? "" : config.WorkDir,
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
        
        if (env is not null)
        {
            foreach (var environmentVariable in env)
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

    private static IEnumerable<string> ProcessTemplate(IEnumerable<string> templatedLines)
    {
        foreach (var line in templatedLines)
        {
            var outputLine = line;
            while (true)
            {
                var templateStart = outputLine.IndexOf("${{", StringComparison.Ordinal);
                var templateEnd = outputLine.IndexOf("}}", StringComparison.Ordinal);
                if (templateStart >= 0 && templateEnd > templateStart)
                {
                    var start = templateStart + 3;
                    var end = templateEnd;
                    var value = outputLine.Substring(start, end - start);
                    var trimmedValue = value.Trim();
                    var replacement = "";
                    if(trimmedValue.StartsWith("env.", StringComparison.OrdinalIgnoreCase))
                    {
                        var envVariableName = trimmedValue.Substring(4);
                        
                        replacement = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                            ? $"$Env:{envVariableName}" 
                            : $"${envVariableName}";
                    }
                    outputLine = outputLine.Replace("${{" + value + "}}", replacement);
                }
                else
                {
                    break;
                }
            }

            yield return outputLine;
        }
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